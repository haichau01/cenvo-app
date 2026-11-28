namespace Cenvo.Modules.Schedule.Contracts;

/// <remarks>Built in SQL → init properties.</remarks>
public record RosterShift
{
    public required Guid AssignmentId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required DateOnly Date { get; init; }
    public required string ShiftName { get; init; }
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }
}

/// <summary>Planned shifts, for attendance to compare punches against.</summary>
public interface IShiftRoster
{
    /// <summary>
    /// The person's shifts in <paramref name="branchId"/> that are running at <paramref name="at"/> or start within
    /// <paramref name="aheadOf"/> — the branch comes from the punch (QR code / shop POS), not from the person's session.
    /// </summary>
    Task<List<RosterShift>> Around(Guid branchId, Guid employeeId, DateTime at, TimeSpan aheadOf, CancellationToken ct);

    /// <summary>Shifts of the current branch on these days (inclusive), ordered by start.</summary>
    Task<List<RosterShift>> Between(DateOnly from, DateOnly to, Guid? employeeId, CancellationToken ct);

    /// <summary>One person's own shifts in every branch on these days (inclusive), ordered by start.</summary>
    Task<List<RosterShift>> OfEmployee(Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>A shift of the current branch.</summary>
    Task<RosterShift?> Find(Guid assignmentId, CancellationToken ct);
}
