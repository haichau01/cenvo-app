namespace Cenvo.SharedKernel;

/// <summary>Store UTC; display and cut days in Vietnam time.</summary>
public static class VnTime
{
    /// <summary>IANA id; also usable inside EF queries (Npgsql translates ConvertTimeBySystemTimeZoneId to AT TIME ZONE).</summary>
    public const string ZoneId = "Asia/Ho_Chi_Minh";

    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(ZoneId);

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>UTC instant at which the day starts (Vietnam time).</summary>
    public static DateTime StartOfDayUtc(DateOnly d) =>
        TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), Zone);

    public static DateOnly Today => DateOnly.FromDateTime(ToLocal(DateTime.UtcNow));
}
