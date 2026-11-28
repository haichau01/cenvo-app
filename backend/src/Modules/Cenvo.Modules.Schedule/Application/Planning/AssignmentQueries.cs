namespace Cenvo.Modules.Schedule.Application.Planning;

internal static class AssignmentQueries
{
    /// <summary>
    /// Assignments of the given people in every branch (a person's hours and overlaps count chain-wide).
    /// IgnoreQueryFilters: single table, filtered by employee — callers only read times / totals or the person's own rows.
    /// </summary>
    public static IQueryable<ShiftAssignment> OfEmployeesInAllBranches(this AppDbContext db, IReadOnlyCollection<Guid> employeeIds) =>
        db.Set<ShiftAssignment>().IgnoreQueryFilters().AsNoTracking().Where(a => employeeIds.Contains(a.EmployeeId));

    public static IQueryable<ShiftAssignment> Between(this IQueryable<ShiftAssignment> assignments, DateTime fromUtc, DateTime toUtc) =>
        assignments.Where(a => a.StartsAt < toUtc && a.EndsAt > fromUtc);

    /// <summary>Hours per person that start within the week (Vietnam time), summed in SQL.</summary>
    public static async Task<Dictionary<Guid, decimal>> WeeklyHours(this AppDbContext db, IReadOnlyCollection<Guid> employeeIds,
        DateOnly weekStart, CancellationToken ct)
    {
        var from = VnTime.StartOfDayUtc(weekStart);
        var to = VnTime.StartOfDayUtc(weekStart.AddDays(7));
        var rows = await db.OfEmployeesInAllBranches(employeeIds)
            .Where(a => a.StartsAt >= from && a.StartsAt < to)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Minutes = g.Sum(a => a.Minutes) })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.EmployeeId, r => Math.Round(r.Minutes / 60m, 2));
    }
}
