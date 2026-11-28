using Cenvo.Modules.Attendance.Application.Holidays;

namespace Cenvo.Modules.Attendance.Application.Review;

/// <summary>Monthly timesheets: the branch's (managers) and one's own across branches (everyone).</summary>
public class TimesheetQueryService(AppDbContext db, ICurrentUser user, IShiftRoster roster, IBranchDirectory branches)
{
    /// <summary>A month of a busy branch fits well under this.</summary>
    public const int MaxRows = 5000;

    public async Task<TimesheetDto> Branch(DateOnly month, CancellationToken ct)
    {
        var (first, last) = Period(month);
        var now = DateTime.UtcNow;
        var holidays = await db.Between(first, last, ct);
        var records = await db.Set<AttendanceRecord>().AsNoTracking()
            .Where(r => r.WorkDate >= first && r.WorkDate <= last)
            .OrderBy(r => r.CheckInAt)
            .Take(MaxRows)
            .ToListAsync(ct);
        var shifts = await roster.Between(first, last, employeeId: null, ct);

        var names = shifts.GroupBy(s => s.EmployeeId).ToDictionary(g => g.Key, g => g.Last().EmployeeName);
        foreach (var r in records) names[r.EmployeeId] = r.EmployeeName; // the latest spelling wins
        var punchesOf = records.ToLookup(r => r.EmployeeId);
        var shiftsOf = shifts.ToLookup(s => s.EmployeeId);
        var multipliers = holidays.Multipliers();
        var rows = names
            .Select(n => new TimesheetRowDto(n.Key, n.Value, Totals(punchesOf[n.Key], shiftsOf[n.Key], multipliers, now)))
            .OrderBy(r => r.EmployeeName)
            .ToList();
        return new TimesheetDto(first, last.Day, holidays, rows);
    }

    /// <summary>
    /// The signed-in person's own month in every branch.
    /// IgnoreQueryFilters: single table, filtered by the person — only their own punches.
    /// </summary>
    public async Task<MyAttendanceDto> Mine(DateOnly month, CancellationToken ct)
    {
        var (first, last) = Period(month);
        var me = user.UserId ?? throw new InvalidOperationException("Signed-in user without an id.");
        var now = DateTime.UtcNow;
        var holidays = await db.Between(first, last, ct);
        var records = await db.Set<AttendanceRecord>().IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.EmployeeId == me && r.WorkDate >= first && r.WorkDate <= last)
            .OrderBy(r => r.CheckInAt)
            .Take(Limits.MaxListSize)
            .ToListAsync(ct);
        var shifts = await roster.OfEmployee(me, first, last, ct);

        var branchNames = new Dictionary<Guid, string>();
        foreach (var branchId in records.Select(r => r.BranchId).Distinct()) // a person works at one or two branches
            branchNames[branchId] = (await branches.Contact(branchId, ct))?.Name ?? "";
        var rows = records.Select(r => AttendanceRowDto.From(r, editCount: 0, now) with { BranchName = branchNames[r.BranchId] }).ToList();
        return new MyAttendanceDto(first, last.Day, holidays, Totals(records, shifts, holidays.Multipliers(), now), rows);
    }

    private static TimesheetTotals Totals(IEnumerable<AttendanceRecord> records, IEnumerable<RosterShift> shifts,
        IReadOnlyDictionary<DateOnly, decimal> holidays, DateTime now) =>
        Timesheet.Calculate(
            records.Select(r => PunchFact.From(r, now)).ToList(),
            shifts.Select(s => new PlannedFact(s.AssignmentId, s.EndsAt <= now)).ToList(),
            holidays);

    private static (DateOnly First, DateOnly Last) Period(DateOnly month)
    {
        if (month.Year is < 2000 or > 2100) throw new BusinessException(AttendanceErrors.InvalidMonth);
        var first = new DateOnly(month.Year, month.Month, 1);
        return (first, first.AddMonths(1).AddDays(-1));
    }
}
