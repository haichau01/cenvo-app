namespace Cenvo.Modules.Inventory.Posting;

/// <summary>Write-off / internal use: deducts stock at the moving-average cost.</summary>
public class IssuePostingStrategy(StockService stock) : IStockPostingStrategy
{
    public bool Handles(StockDocumentType type) =>
        type is StockDocumentType.DisposalIssue or StockDocumentType.InternalIssue;

    public Task<List<StockMovement>> Post(StockDocument doc, bool allowNegative, CancellationToken ct) =>
        stock.Post(doc, doc.Lines.Select(l => new MovementRequest(doc.WarehouseId, l.ProductId, -l.BaseQuantity)),
            allowNegative, isReversal: false, ct);
}
