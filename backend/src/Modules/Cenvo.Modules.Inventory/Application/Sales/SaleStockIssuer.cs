using Cenvo.Modules.Inventory.Application.Warehouses;

namespace Cenvo.Modules.Inventory.Application.Sales;

public class SaleStockIssuer(
    AppDbContext db,
    StockService stock,
    WarehouseService warehouses,
    IUnitConverterFactory converters,
    IDocumentNumberGenerator numbers) : ISaleStockIssuer
{
    public async Task IssueForSale(Guid branchId, string refType, Guid refId, string refCode,
        IEnumerable<SaleIssueLine> lines, CancellationToken ct)
    {
        var merged = lines.GroupBy(l => l.ProductId)
            .Select(g => new SaleIssueLine(g.Key, g.Sum(l => l.BaseQuantity)))
            .Where(l => l.BaseQuantity != 0).ToList();
        var converter = await converters.Create(merged.Select(l => l.ProductId), ct);
        merged = merged.Where(l => converter.Product(l.ProductId).TrackStock).ToList();
        if (merged.Count == 0) return;

        var now = DateTime.UtcNow;
        var warehouseId = await warehouses.DefaultWarehouseId(branchId, ct);
        var code = await numbers.Next(branchId, StockDocumentType.SaleIssue.CodePrefix(), now, ct);
        var doc = StockDocument.CreateSaleIssue(branchId, code, warehouseId, refType, refId, refCode, now);
        doc.Lines.AddRange(merged.Select(l => StockDocumentLine.Create(l.ProductId, null, l.BaseQuantity, l.BaseQuantity, 0, null)));
        db.Add(doc);

        // Sales may always drive stock negative (never block a customer); negative stock shows in alerts / stock reports.
        var movements = await stock.Post(doc, merged.Select(l => new MovementRequest(warehouseId, l.ProductId, -l.BaseQuantity)),
            allowNegative: true, isReversal: false, ct);

        // Capture cost on the document line so profit reports don't need to recompute it.
        var costByProduct = movements.ToDictionary(m => m.ProductId, m => m.UnitCost);
        foreach (var line in doc.Lines) line.SetCost(costByProduct[line.ProductId]);
    }

    public async Task CancelByRef(string refType, Guid refId, string reason, CancellationToken ct)
    {
        var docs = await db.Set<StockDocument>()
            .IgnoreQueryFilters() // cancel by source document, regardless of the currently selected branch
            .Where(d => d.RefType == refType && d.RefId == refId && d.Status == DocumentStatus.Posted)
            .ToListAsync(ct);
        foreach (var doc in docs)
        {
            doc.MarkCancelled(reason);
            await stock.Reverse(doc, ct);
        }
    }
}
