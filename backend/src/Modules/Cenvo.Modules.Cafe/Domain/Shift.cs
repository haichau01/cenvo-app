namespace Cenvo.Modules.Cafe.Domain;

/// <summary>Cashier shift: opens with a cash float, closes with a cash count reconciliation.</summary>
public class Shift : BaseEntity, IBranchScoped
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = "";
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public DateTime OpenedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime? ClosedAt { get; set; }
    /// <summary>Expected cash: opening float + cash taken.</summary>
    public decimal? ExpectedCash { get; set; }
    /// <summary>Cash actually counted at close.</summary>
    public decimal? CountedCash { get; set; }
    public string? Note { get; set; }

    public static Shift Open(Guid branchId, string code, Guid cashierId, string cashierName, decimal openingCash, DateTime now) =>
        new()
        {
            BranchId = branchId, Code = code, CashierId = cashierId, CashierName = cashierName,
            OpeningCash = openingCash, OpenedAt = now,
        };

    public void Close(decimal expectedCash, decimal countedCash, string? note, DateTime now)
    {
        if (Status != ShiftStatus.Open) throw new BusinessException(CafeErrors.NoOpenShift);
        Status = ShiftStatus.Closed;
        ClosedAt = now;
        ExpectedCash = expectedCash;
        CountedCash = countedCash;
        Note = note;
    }
}
