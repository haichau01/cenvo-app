namespace Cenvo.Modules.Inventory.Posting;

/// <summary>Stocktake: snapshots the book quantity onto each line and posts the difference (counted − book).</summary>
public class StocktakePostingStrategy(StockService stock) : IStockPostingStrategy
{
    public bool Handles(StockDocumentType type) => type == StockDocumentType.Stocktake;

    public async Task<List<StockMovement>> Post(StockDocument doc, bool allowNegative, CancellationToken ct)
    {
        var onHand = await stock.OnHand(doc.WarehouseId, doc.Lines.Select(l => l.ProductId), ct);
        foreach (var line in doc.Lines) line.SystemQuantity = onHand.GetValueOrDefault(line.ProductId);

        var adjustments = doc.Lines.Select(l => new MovementRequest(doc.WarehouseId, l.ProductId, l.BaseQuantity - l.SystemQuantity!.Value));
        return await stock.Post(doc, adjustments, allowNegative: true, isReversal: false, ct);
    }
}
