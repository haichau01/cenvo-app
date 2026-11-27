namespace Cenvo.Modules.Inventory.Application.Stock;

public class StockQueryService(AppDbContext db)
{
    // Never use IgnoreQueryFilters in the joins below: it disables the branch filter for the WHOLE query.
    public Task<List<BalanceDto>> Balances(Guid? warehouseId, string? search, bool belowMinOnly, CancellationToken ct)
    {
        var query =
            from b in db.Set<StockBalance>().AsNoTracking()
            join p in db.Set<Product>() on b.ProductId equals p.Id
            join w in db.Set<Warehouse>() on b.WarehouseId equals w.Id
            join u in db.Set<Unit>() on p.BaseUnitId equals u.Id
            where warehouseId == null || b.WarehouseId == warehouseId
            select new { b, p, w, u };
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.p.Name, $"%{search}%") || EF.Functions.ILike(x.p.Code, $"%{search}%"));
        if (belowMinOnly) query = query.Where(x => x.p.MinStock > 0 && x.b.Quantity < x.p.MinStock);

        return query.OrderBy(x => x.w.Name).ThenBy(x => x.p.Code).Take(Limits.MaxListSize)
            .Select(x => new BalanceDto(x.w.Id, x.w.Name, x.p.Id, x.p.Code, x.p.Name, x.p.Kind, x.u.Name, x.b.Quantity,
                x.b.AverageCost, Math.Round(x.b.Quantity * x.b.AverageCost, 0), x.p.MinStock,
                x.p.MinStock > 0 && x.b.Quantity < x.p.MinStock))
            .ToListAsync(ct);
    }

    /// <summary>Products with a minimum level whose stock is below it — including products never received.</summary>
    public async Task<List<BalanceDto>> LowStock(CancellationToken ct)
    {
        var warehouses = await db.Set<Warehouse>().AsNoTracking().ToListAsync(ct);
        var products = await db.Set<Product>().AsNoTracking().Include(p => p.BaseUnit)
            .Where(p => p.TrackStock && p.MinStock > 0 && p.IsActive).ToListAsync(ct);
        var balances = await db.Set<StockBalance>().AsNoTracking()
            .ToDictionaryAsync(b => (b.WarehouseId, b.ProductId), ct);

        return (from w in warehouses
                from p in products
                let balance = balances.GetValueOrDefault((w.Id, p.Id))
                let quantity = balance?.Quantity ?? 0
                let cost = balance?.AverageCost ?? 0
                where quantity < p.MinStock
                orderby w.Name, p.Code
                select new BalanceDto(w.Id, w.Name, p.Id, p.Code, p.Name, p.Kind, p.BaseUnit!.Name, quantity, cost,
                    Math.Round(quantity * cost, 0), p.MinStock, BelowMin: true)).ToList();
    }

    public async Task<StockCardDto> StockCard(Guid productId, Guid warehouseId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var product = await db.Set<Product>().IgnoreQueryFilters().Include(p => p.BaseUnit)
                          .FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("sản phẩm", productId);
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));

        var movements = db.Set<StockMovement>().AsNoTracking().Where(m => m.ProductId == productId && m.WarehouseId == warehouseId);
        var opening = await movements.Where(m => m.Date < fromUtc).SumAsync(m => m.Quantity, ct);
        var inRange = await movements.Where(m => m.Date >= fromUtc && m.Date < toUtc)
            .OrderBy(m => m.Date).ThenBy(m => m.CreatedAt).ToListAsync(ct);

        var running = opening;
        var rows = inRange.Select(m =>
        {
            running += m.Quantity;
            return new StockCardRow(m.Date, m.DocumentCode, m.DocumentType, m.DocumentId, m.IsReversal,
                In: Math.Max(m.Quantity, 0), Out: Math.Max(-m.Quantity, 0), m.UnitCost, running);
        }).ToList();
        return new StockCardDto(product.Id, product.Name, product.BaseUnit!.Name, opening,
            rows.Sum(r => r.In), rows.Sum(r => r.Out), running, rows);
    }

    /// <summary>Ingredients consumed by sales (net of quantities returned by cancelled orders).</summary>
    public async Task<List<ConsumptionDto>> Consumption(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));
        var totals = await db.Set<StockMovement>().AsNoTracking()
            .Where(m => m.DocumentType == StockDocumentType.SaleIssue && m.Date >= fromUtc && m.Date < toUtc)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = -g.Sum(m => m.Quantity), Cost = -g.Sum(m => m.Quantity * m.UnitCost) })
            .Where(x => x.Quantity != 0)
            .ToListAsync(ct);

        var ids = totals.Select(t => t.ProductId).ToList();
        var products = await db.Set<Product>().IgnoreQueryFilters().Include(p => p.BaseUnit)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        return totals.Select(t => new ConsumptionDto(t.ProductId, products[t.ProductId].Code, products[t.ProductId].Name,
                products[t.ProductId].BaseUnit!.Name, t.Quantity, Math.Round(t.Cost, 0)))
            .OrderByDescending(c => c.Cost).ToList();
    }
}
