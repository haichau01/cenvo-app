using Cenvo.Modules.Catalog.Application.Products;

namespace Cenvo.Modules.Inventory.Application.Stock;

public class StockQueryService(AppDbContext db)
{
    // Never use IgnoreQueryFilters in the joins below: it disables the branch filter for the WHOLE query.
    public Task<List<BalanceDto>> Balances(Guid? warehouseId, string? search, bool belowMinOnly, CancellationToken ct)
    {
        var products = db.Set<Product>().Matching(search);
        var rows =
            from balance in db.Set<StockBalance>().AsNoTracking()
            join product in products on balance.ProductId equals product.Id
            join warehouse in db.Set<Warehouse>() on balance.WarehouseId equals warehouse.Id
            where warehouseId == null || balance.WarehouseId == warehouseId
            select new BalanceRow
            {
                Warehouse = warehouse, Product = product, Quantity = balance.Quantity, AverageCost = balance.AverageCost,
            };
        if (belowMinOnly) rows = rows.Where(BalanceRow.IsBelowMin);

        return ToSortedDtos(rows, ct);
    }

    /// <summary>Products with a minimum level whose stock is below it — including products never received (no balance row).</summary>
    public Task<List<BalanceDto>> LowStock(CancellationToken ct)
    {
        var watchedProducts = db.Set<Product>().AsNoTracking().ActiveOnly().Where(p => p.TrackStock && p.MinStock > 0);
        var rows =
            // Every warehouse × every watched product, so missing balances count as zero stock.
            from warehouse in db.Set<Warehouse>().AsNoTracking()
            from product in watchedProducts
            join balance in db.Set<StockBalance>()
                on new { WarehouseId = warehouse.Id, ProductId = product.Id } equals new { balance.WarehouseId, balance.ProductId }
                into found
            from balance in found.DefaultIfEmpty()
            select new BalanceRow
            {
                Warehouse = warehouse, Product = product,
                Quantity = balance == null ? 0 : balance.Quantity,
                AverageCost = balance == null ? 0 : balance.AverageCost,
            };

        return ToSortedDtos(rows.Where(BalanceRow.IsBelowMin), ct);
    }

    public async Task<StockCardDto> StockCard(Guid productId, Guid warehouseId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var product = await db.Set<Product>()
                          .IgnoreQueryFilters() // Product is not branch-scoped; discontinued (soft-deleted) products keep their history
                          .Include(p => p.BaseUnit)
                          .FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("sản phẩm", productId);
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));

        var movements = db.Set<StockMovement>().AsNoTracking().Where(m => m.ProductId == productId && m.WarehouseId == warehouseId);
        var opening = await movements.Where(m => m.Date < fromUtc).SumAsync(m => m.Quantity, ct);
        var inRange = await movements.Where(m => m.Date >= fromUtc && m.Date < toUtc)
            .OrderBy(m => m.Date).ThenBy(m => m.CreatedAt).ToListAsync(ct);

        var rows = RunningBalance(opening, inRange);
        return new StockCardDto(
            ProductId: product.Id, ProductName: product.Name, UnitName: product.BaseUnit!.Name, Opening: opening,
            TotalIn: rows.Sum(r => r.In), TotalOut: rows.Sum(r => r.Out),
            Closing: rows.Count == 0 ? opening : rows[^1].Balance, Rows: rows);
    }

    /// <summary>Ingredients consumed by sales (net of quantities returned by cancelled orders).</summary>
    public async Task<List<ConsumptionDto>> Consumption(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));
        var totals = await db.Set<StockMovement>().AsNoTracking()
            .Where(m => m.DocumentType == StockDocumentType.SaleIssue && m.Date >= fromUtc && m.Date < toUtc)
            .GroupBy(m => m.ProductId)
            // Sale issues are negative movements: flip the sign to report consumption as positive numbers.
            .Select(g => new { ProductId = g.Key, Quantity = -g.Sum(m => m.Quantity), Cost = -g.Sum(m => m.Quantity * m.UnitCost) })
            .Where(x => x.Quantity != 0)
            .ToListAsync(ct);

        var ids = totals.Select(t => t.ProductId).ToList();
        var products = await db.Set<Product>()
            .IgnoreQueryFilters() // Product is not branch-scoped; soft-deleted ingredients were still consumed
            .Include(p => p.BaseUnit)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        return totals
            .Select(t =>
            {
                var product = products[t.ProductId];
                return new ConsumptionDto(ProductId: product.Id, ProductCode: product.Code, ProductName: product.Name,
                    UnitName: product.BaseUnit!.Name, Quantity: t.Quantity, Cost: Math.Round(t.Cost, 0));
            })
            .OrderByDescending(c => c.Cost)
            .ToList();
    }

    private static Task<List<BalanceDto>> ToSortedDtos(IQueryable<BalanceRow> rows, CancellationToken ct) =>
        rows.OrderBy(row => row.Warehouse.Name)
            .ThenBy(row => row.Product.Code)
            .Take(Limits.MaxListSize)
            .Select(row => new BalanceDto
            {
                WarehouseId = row.Warehouse.Id,
                WarehouseName = row.Warehouse.Name,
                ProductId = row.Product.Id,
                ProductCode = row.Product.Code,
                ProductName = row.Product.Name,
                Kind = row.Product.Kind,
                UnitName = row.Product.BaseUnit!.Name,
                Quantity = row.Quantity,
                AverageCost = row.AverageCost,
                Value = Math.Round(row.Quantity * row.AverageCost, 0),
                MinStock = row.Product.MinStock,
                BelowMin = row.Product.MinStock > 0 && row.Quantity < row.Product.MinStock, // same rule as BalanceRow.IsBelowMin
            })
            .ToListAsync(ct);

    private static List<StockCardRow> RunningBalance(decimal opening, IEnumerable<StockMovement> movements)
    {
        var balance = opening;
        return movements.Select(m =>
        {
            balance += m.Quantity;
            return new StockCardRow(Date: m.Date, DocumentCode: m.DocumentCode, DocumentType: m.DocumentType,
                DocumentId: m.DocumentId, IsReversal: m.IsReversal, In: Math.Max(m.Quantity, 0), Out: Math.Max(-m.Quantity, 0),
                UnitCost: m.UnitCost, Balance: balance);
        }).ToList();
    }

    /// <summary>Intermediate row shared by the balance queries (still translated to SQL).</summary>
    private class BalanceRow
    {
        public required Warehouse Warehouse { get; init; }
        public required Product Product { get; init; }
        public decimal Quantity { get; init; }
        public decimal AverageCost { get; init; }

        /// <summary>Below the product's minimum level (products without a minimum are never "below").</summary>
        public static readonly System.Linq.Expressions.Expression<Func<BalanceRow, bool>> IsBelowMin =
            row => row.Product.MinStock > 0 && row.Quantity < row.Product.MinStock;
    }
}
