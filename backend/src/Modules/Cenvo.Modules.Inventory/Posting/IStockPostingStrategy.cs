namespace Cenvo.Modules.Inventory.Posting;

/// <summary>How the ledger is written when posting one document type. New document type = new class implementing this interface.</summary>
public interface IStockPostingStrategy
{
    bool Handles(StockDocumentType type);
    Task<List<StockMovement>> Post(StockDocument doc, bool allowNegative, CancellationToken ct);
}
