using System.ComponentModel.DataAnnotations;

namespace Cenvo.Modules.Hr.Domain;

/// <summary>
/// Advance, bonus or penalty for one employee, recorded at a branch (with a reason). Feeds the monthly payroll later.
/// Money record → never deleted, only cancelled with a reason.
/// </summary>
public class PayAdjustment : BaseEntity, IBranchScoped
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public AdjustmentKind Kind { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Day it happened (Vietnam date) — decides the pay period.</summary>
    public DateOnly Date { get; set; }
    public string Reason { get; set; } = "";
    public string RecordedByName { get; set; } = "";
    public AdjustmentStatus Status { get; set; } = AdjustmentStatus.Active;
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancelReason { get; set; }
    [Timestamp] public uint Version { get; set; }

    public static PayAdjustment Record(Guid employeeId, AdjustmentKind kind, decimal amount, DateOnly date, string? reason,
        string recordedByName, DateOnly today)
    {
        if (amount <= 0) throw new BusinessException(HrErrors.InvalidAmount);
        if (date > today) throw new BusinessException(HrErrors.AdjustmentDateInFuture);
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException(HrErrors.ReasonRequired);
        return new PayAdjustment
        {
            EmployeeId = employeeId, Kind = kind, Amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero), Date = date,
            Reason = reason.Trim(), RecordedByName = recordedByName,
        };
    }

    /// <param name="byOwner">Admin may cancel any entry; others only what they recorded.</param>
    public void Cancel(string? reason, Guid? by, bool byOwner, DateTime now)
    {
        if (Status == AdjustmentStatus.Cancelled) throw new BusinessException(HrErrors.AdjustmentCancelled);
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException(HrErrors.ReasonRequired);
        if (!byOwner && (by == null || by != CreatedBy)) throw new BusinessException(HrErrors.CancelOthersAdjustment);
        Status = AdjustmentStatus.Cancelled;
        CancelledAt = now;
        CancelledBy = by;
        CancelReason = reason.Trim();
    }
}
