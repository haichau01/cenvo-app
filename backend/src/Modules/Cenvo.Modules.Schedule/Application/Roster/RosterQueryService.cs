using System.Linq.Expressions;
using Cenvo.Modules.Schedule.Application.Planning;

namespace Cenvo.Modules.Schedule.Application.Roster;

/// <summary>Read side of <see cref="IShiftRoster"/>.</summary>
public class RosterQueryService(AppDbContext db) : IShiftRoster
{
    public const int MaxDays = 31;
    /// <summary>A month of a busy branch (30 people × 2 shifts × 31 days) fits well under this.</summary>
    public const int MaxRows = 5000;

    private static readonly Expression<Func<ShiftAssignment, RosterShift>> ToRoster = a => new RosterShift
    {
        AssignmentId = a.Id, BranchId = a.BranchId, EmployeeId = a.EmployeeId, EmployeeName = a.EmployeeName, Date = a.Date,
        ShiftName = a.ShiftName, StartsAt = a.StartsAt, EndsAt = a.EndsAt,
    };

    public Task<List<RosterShift>> Around(Guid branchId, Guid employeeId, DateTime at, TimeSpan aheadOf, CancellationToken ct)
    {
        var startsBy = at.Add(aheadOf);
        return db.OfEmployeesInAllBranches([employeeId])
            .Where(a => a.BranchId == branchId && a.StartsAt <= startsBy && a.EndsAt > at)
            .OrderBy(a => a.StartsAt)
            .Select(ToRoster)
            .ToListAsync(ct);
    }

    public Task<List<RosterShift>> Between(DateOnly from, DateOnly to, Guid? employeeId, CancellationToken ct)
    {
        EnsureRange(from, to);
        var shifts = db.Set<ShiftAssignment>().AsNoTracking().Where(a => a.Date >= from && a.Date <= to);
        if (employeeId != null) shifts = shifts.Where(a => a.EmployeeId == employeeId);
        return shifts.OrderBy(a => a.StartsAt).Take(MaxRows).Select(ToRoster).ToListAsync(ct);
    }

    public Task<List<RosterShift>> OfEmployee(Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        EnsureRange(from, to);
        return db.OfEmployeesInAllBranches([employeeId])
            .Where(a => a.Date >= from && a.Date <= to)
            .OrderBy(a => a.StartsAt).Take(MaxRows).Select(ToRoster).ToListAsync(ct);
    }

    public Task<RosterShift?> Find(Guid assignmentId, CancellationToken ct) =>
        db.Set<ShiftAssignment>().AsNoTracking().Where(a => a.Id == assignmentId).Select(ToRoster).FirstOrDefaultAsync(ct);

    private static void EnsureRange(DateOnly from, DateOnly to)
    {
        if (to < from || to.DayNumber - from.DayNumber > MaxDays) throw new BusinessException(ScheduleErrors.InvalidRange);
    }
}
