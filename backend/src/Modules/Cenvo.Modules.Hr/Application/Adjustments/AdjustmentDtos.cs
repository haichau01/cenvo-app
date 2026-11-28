namespace Cenvo.Modules.Hr.Application.Adjustments;

public record AdjustmentInput(Guid EmployeeId, AdjustmentKind Kind, decimal Amount, DateOnly Date, string Reason);

public record CancelAdjustmentInput(string Reason);

/// <remarks>Built in SQL → init properties (employee name is filled in afterwards from the accounts).</remarks>
public record AdjustmentDto
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public string EmployeeName { get; init; } = "";
    public required AdjustmentKind Kind { get; init; }
    public required decimal Amount { get; init; }
    public required DateOnly Date { get; init; }
    public required string Reason { get; init; }
    public required string RecordedByName { get; init; }
    public required Guid? RecordedBy { get; init; }
    public required AdjustmentStatus Status { get; init; }
    public required string? CancelReason { get; init; }
    /// <summary>The current user may cancel it (owner: any; others: what they recorded).</summary>
    public bool CanCancel { get; init; }
}
