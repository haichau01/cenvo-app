namespace Cenvo.Modules.Inventory.Posting;

/// <summary>Purchase receipt / opening balance: adds stock with cost = line amount ÷ base quantity.</summary>
public class ReceiptPostingStrategy(StockService stock) : IStockPostingStrategy
{
    public bool Handles(StockDocumentType type) =>
        type is StockDocumentType.PurchaseReceipt or StockDocumentType.OpeningBalance;

    public Task<List<StockMovement>> Post(StockDocument doc, bool allowNegative, CancellationToken ct) =>
        stock.Post(doc, doc.Lines.Select(l => new MovementRequest(doc.WarehouseId, l.ProductId, l.BaseQuantity, l.BaseUnitCost)),
            allowNegative, isReversal: false, ct);
}
