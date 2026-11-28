namespace Cenvo.Modules.Schedule.Application.Planning;

/// <summary>Any signed-in person: their own shifts in every branch they work at.</summary>
public class MyScheduleQueryService(AppDbContext db, ICurrentUser user, IBranchDirectory branches)
{
    public const int MaxDays = 31;

    public async Task<List<MyShiftDto>> Mine(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from || to.DayNumber - from.DayNumber > MaxDays) throw new BusinessException(ScheduleErrors.InvalidRange);
        var me = user.UserId ?? throw new InvalidOperationException("Signed-in user without an id.");

        var shifts = await db.OfEmployeesInAllBranches([me])
            .Where(a => a.Date >= from && a.Date <= to)
            .OrderBy(a => a.StartsAt)
            .Select(a => new MyShiftDto
            {
                Id = a.Id, Date = a.Date, ShiftName = a.ShiftName, StartTime = a.StartTime, EndTime = a.EndTime, BranchId = a.BranchId,
            })
            .ToListAsync(ct);

        var names = new Dictionary<Guid, string>();
        foreach (var branchId in shifts.Select(s => s.BranchId).Distinct()) // a person works at one or two branches
            names[branchId] = (await branches.Contact(branchId, ct))?.Name ?? "";
        return shifts.Select(s => s with { BranchName = names[s.BranchId] }).ToList();
    }
}
