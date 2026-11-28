namespace Cenvo.Modules.Attendance.Application.Holidays;

internal static class HolidayQueries
{
    public static Task<List<HolidayDto>> Between(this AppDbContext db, DateOnly from, DateOnly to, CancellationToken ct) =>
        db.Set<Holiday>().AsNoTracking()
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto { Id = h.Id, Date = h.Date, Name = h.Name, Multiplier = h.Multiplier })
            .ToListAsync(ct);

    public static Dictionary<DateOnly, decimal> Multipliers(this IEnumerable<HolidayDto> holidays) =>
        holidays.ToDictionary(h => h.Date, h => h.Multiplier);
}
