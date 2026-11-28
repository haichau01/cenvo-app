namespace Cenvo.Modules.Schedule.Domain.Calculations;

/// <summary>When a shift actually runs, in UTC. A shift whose end is not after its start runs past midnight.</summary>
public readonly record struct ShiftWindow(DateTime StartUtc, DateTime EndUtc)
{
    public static ShiftWindow On(DateOnly date, TimeOnly start, TimeOnly end)
    {
        var endDate = end <= start ? date.AddDays(1) : date;
        return new ShiftWindow(ToUtc(date, start), ToUtc(endDate, end));
    }

    /// <summary>TimeOnly subtraction wraps past midnight: 06:00 − 22:00 = 8 h.</summary>
    public static TimeSpan Length(TimeOnly start, TimeOnly end) => end - start;

    /// <summary>Back-to-back shifts (one ends when the next starts) do not overlap.</summary>
    public bool Overlaps(ShiftWindow other) => StartUtc < other.EndUtc && other.StartUtc < EndUtc;

    public decimal Hours => Math.Round((decimal)(EndUtc - StartUtc).TotalHours, 2);

    private static DateTime ToUtc(DateOnly date, TimeOnly time) => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time), VnTime.Zone);
}
