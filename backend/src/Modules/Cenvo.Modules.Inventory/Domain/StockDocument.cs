namespace Cenvo.Modules.Inventory.Domain;

public class StockDocument : BaseEntity, IBranchScoped
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public StockDocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime Date { get; set; }
    public Guid WarehouseId { get; set; }
    /// <summary>Destination warehouse (transfers only).</summary>
    public Guid? ToWarehouseId { get; set; }
    public TransferState TransferState { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Note { get; set; }
    /// <summary>Source document that produced this one (e.g. "CafeOrder").</summary>
    public string? RefType { get; set; }
    public Guid? RefId { get; set; }
    public string? RefCode { get; set; }
    public string? CancelReason { get; set; }
    public List<StockDocumentLine> Lines { get; set; } = [];

    public bool IsAwaitingReceipt =>
        Type == StockDocumentType.Transfer && Status == DocumentStatus.Posted && TransferState == TransferState.InTransit;

    public static StockDocument CreateDraft(Guid branchId, string code, StockDocumentType type) =>
        new() { BranchId = branchId, Code = code, Type = type, Status = DocumentStatus.Draft };

    public static StockDocument CreateSaleIssue(Guid branchId, string code, Guid warehouseId,
        string refType, Guid refId, string refCode, DateTime now) => new()
    {
        BranchId = branchId, Code = code, Type = StockDocumentType.SaleIssue, Status = DocumentStatus.Posted,
        Date = now, WarehouseId = warehouseId, RefType = refType, RefId = refId, RefCode = refCode,
        Note = $"Xuất bán theo {refCode}",
    };

    public void SetHeader(Guid warehouseId, Guid? toWarehouseId, Guid? supplierId, DateTime dateUtc, string? note)
    {
        EnsureDraft();
        if (Type == StockDocumentType.Transfer && (toWarehouseId == null || toWarehouseId == warehouseId))
            throw new BusinessException(InventoryErrors.TransferTargetRequired);
        WarehouseId = warehouseId;
        ToWarehouseId = Type == StockDocumentType.Transfer ? toWarehouseId : null;
        SupplierId = Type == StockDocumentType.PurchaseReceipt ? supplierId : null;
        Date = dateUtc;
        Note = note;
    }

    public void ReplaceLines(IReadOnlyCollection<StockDocumentLine> lines)
    {
        EnsureDraft();
        var zeroAllowed = Type == StockDocumentType.Stocktake; // a stocktake count of 0 is valid
        if (lines.Any(l => l.Quantity < 0 || (l.Quantity == 0 && !zeroAllowed)))
            throw new BusinessException(InventoryErrors.QuantityMustBePositive);
        if (Type == StockDocumentType.Stocktake && lines.GroupBy(l => l.ProductId).Any(g => g.Count() > 1))
            throw new BusinessException(InventoryErrors.StocktakeDuplicateProduct);

        Lines.Clear();
        foreach (var line in lines)
        {
            line.DocumentId = Id;
            Lines.Add(line);
        }
    }

    public void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft) throw new BusinessException(InventoryErrors.NotDraft);
    }

    public void MarkPosted()
    {
        EnsureDraft();
        if (Lines.Count == 0) throw new BusinessException(InventoryErrors.NoLines);
        Status = DocumentStatus.Posted;
    }

    public void MarkInTransit() => TransferState = TransferState.InTransit;

    public void MarkReceived(DateTime now)
    {
        if (!IsAwaitingReceipt) throw new BusinessException(InventoryErrors.NotAwaitingReceipt);
        TransferState = TransferState.Received;
        ReceivedAt = now;
    }

    public void MarkCancelled(string reason)
    {
        if (Status != DocumentStatus.Posted) throw new BusinessException(InventoryErrors.OnlyPostedCanBeCancelled);
        Status = DocumentStatus.Cancelled;
        CancelReason = reason;
        if (Type == StockDocumentType.Transfer) TransferState = TransferState.None;
    }
}
