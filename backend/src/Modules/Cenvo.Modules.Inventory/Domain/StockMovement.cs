namespace Cenvo.Modules.Inventory.Domain;

/// <summary>Stock ledger — append-only, never updated or deleted. The single source of truth for stock.</summary>
public class StockMovement : BaseEntity, IBranchScoped
{
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime Date { get; set; }
    /// <summary>+ in, − out (base unit).</summary>
    [Precision(18, 3)] public decimal Quantity { get; set; }
    /// <summary>Base-unit cost at posting time.</summary>
    [Precision(18, 4)] public decimal UnitCost { get; set; }
    /// <summary>Balance after the movement (for fast stock-card printing).</summary>
    [Precision(18, 3)] public decimal BalanceAfter { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentCode { get; set; } = "";
    public StockDocumentType DocumentType { get; set; }
    /// <summary>Reversal entry written when a document is cancelled.</summary>
    public bool IsReversal { get; set; }
}
