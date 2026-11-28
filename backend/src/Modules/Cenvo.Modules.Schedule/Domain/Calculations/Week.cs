namespace Cenvo.Modules.Schedule.Domain.Calculations;

/// <summary>Weeks run Monday → Sunday.</summary>
public static class Week
{
    public static DateOnly StartOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    public static IReadOnlyList<DateOnly> Days(DateOnly start) => Enumerable.Range(0, 7).Select(start.AddDays).ToList();
}
