namespace Cenvo.Modules.Inventory.Posting;

/// <summary>Transfer step 1: deducts the source warehouse; goods stay "in transit" until the destination confirms.</summary>
public class TransferPostingStrategy(StockService stock) : IStockPostingStrategy
{
    public bool Handles(StockDocumentType type) => type == StockDocumentType.Transfer;

    public async Task<List<StockMovement>> Post(StockDocument doc, bool allowNegative, CancellationToken ct)
    {
        var movements = await stock.Post(doc,
            doc.Lines.Select(l => new MovementRequest(doc.WarehouseId, l.ProductId, -l.BaseQuantity)),
            allowNegative, isReversal: false, ct);
        doc.MarkInTransit();
        return movements;
    }
}
