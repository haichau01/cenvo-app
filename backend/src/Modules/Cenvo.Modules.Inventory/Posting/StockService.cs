namespace Cenvo.Modules.Inventory.Posting;

/// <summary>
/// The ONLY place that writes the stock ledger (StockMovement + StockBalance).
/// Does not call SaveChanges — the caller saves in the same transaction as the document.
/// </summary>
public class StockService(AppDbContext db)
{
    public async Task<List<StockMovement>> Post(StockDocument doc, IEnumerable<MovementRequest> requests,
        bool allowNegative, bool isReversal, CancellationToken ct)
    {
        var movements = new List<StockMovement>();
        foreach (var request in requests.Where(r => r.Quantity != 0))
        {
            var balance = await GetOrCreateBalance(request.WarehouseId, request.ProductId, ct);
            var onHand = balance.Quantity;
            var unitCost = request.UnitCost ?? balance.AverageCost;

            if (request.Quantity > 0) balance.Receive(request.Quantity, unitCost);
            else if (isReversal && request.UnitCost != null) balance.ReverseReceipt(-request.Quantity, unitCost);
            else balance.Issue(-request.Quantity);

            if (balance.Quantity < 0 && !allowNegative)
                throw new BusinessException(InventoryErrors.NotEnoughStock(onHand, -request.Quantity));

            balance.UpdatedAt = DateTime.UtcNow;
            movements.Add(Record(doc, balance, request.Quantity, unitCost, isReversal));
        }
        db.AddRange(movements);
        return movements;
    }

    /// <summary>Cancels a document: writes reversal entries for the still-valid movements (per warehouse + product).</summary>
    public async Task Reverse(StockDocument doc, CancellationToken ct)
    {
        var movements = await db.Set<StockMovement>()
            .IgnoreQueryFilters() // transfers have movements in the receiving branch
            .Where(m => m.DocumentId == doc.Id).ToListAsync(ct);

        var reversals = movements.GroupBy(m => new { m.WarehouseId, m.ProductId })
            .Select(g => new MovementRequest(g.Key.WarehouseId, g.Key.ProductId,
                -g.Sum(m => m.Quantity),
                g.Where(m => !m.IsReversal).Select(m => (decimal?)m.UnitCost).FirstOrDefault()))
            .Where(r => r.Quantity != 0);

        await Post(doc, reversals, allowNegative: true, isReversal: true, ct);
    }

    /// <summary>Current stock of several products in one warehouse (one query).</summary>
    public async Task<Dictionary<Guid, decimal>> OnHand(Guid warehouseId, IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToList();
        var result = await db.Set<StockBalance>().IgnoreQueryFilters()
            .Where(b => b.WarehouseId == warehouseId && ids.Contains(b.ProductId))
            .ToDictionaryAsync(b => b.ProductId, b => b.Quantity, ct);
        foreach (var local in db.Set<StockBalance>().Local.Where(b => b.WarehouseId == warehouseId && ids.Contains(b.ProductId)))
            result[local.ProductId] = local.Quantity;
        return result;
    }

    private static StockMovement Record(StockDocument doc, StockBalance balance, decimal quantity, decimal unitCost,
        bool isReversal) => new()
    {
        BranchId = balance.BranchId, WarehouseId = balance.WarehouseId, ProductId = balance.ProductId,
        Date = doc.Date, Quantity = quantity, UnitCost = unitCost, BalanceAfter = balance.Quantity,
        DocumentId = doc.Id, DocumentCode = doc.Code, DocumentType = doc.Type, IsReversal = isReversal,
    };

    // Other branches' warehouses (transfers) must still be writable → IgnoreQueryFilters for this technical lookup.
    private async Task<StockBalance> GetOrCreateBalance(Guid warehouseId, Guid productId, CancellationToken ct)
    {
        var balance = db.Set<StockBalance>().Local.FirstOrDefault(b => b.WarehouseId == warehouseId && b.ProductId == productId)
                      ?? await db.Set<StockBalance>().IgnoreQueryFilters()
                          .FirstOrDefaultAsync(b => b.WarehouseId == warehouseId && b.ProductId == productId, ct);
        if (balance != null) return balance;

        balance = new StockBalance { BranchId = await WarehouseBranch(warehouseId, ct), WarehouseId = warehouseId, ProductId = productId };
        db.Add(balance);
        return balance;
    }

    private async Task<Guid> WarehouseBranch(Guid warehouseId, CancellationToken ct) =>
        db.Set<Warehouse>().Local.FirstOrDefault(w => w.Id == warehouseId)?.BranchId
        ?? await db.Set<Warehouse>().IgnoreQueryFilters().Where(w => w.Id == warehouseId)
            .Select(w => (Guid?)w.BranchId).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("kho", warehouseId);
}
