using System.Globalization;

namespace Cenvo.Modules.CarWash.Application.Reminders;

internal static class ReminderMessages
{
    /// <summary>The branch's settings, or the defaults while the manager has not saved any.</summary>
    public static async Task<ReminderSettings> ReminderSettingsFor(this AppDbContext db, Guid branchId, CancellationToken ct) =>
        await db.Set<ReminderSettings>().FirstOrDefaultAsync(s => s.BranchId == branchId, ct)
        ?? new ReminderSettings { BranchId = branchId };

    public static string Render(string template, DueReminderRow row, BranchContact? branch, DateTime now) =>
        ReminderTemplate.Render(template, new Dictionary<string, string?>
        {
            [ReminderTemplate.CustomerName] = row.CustomerName,
            [ReminderTemplate.Plate] = row.Plate is { } plate ? PlateNumber.Format(plate) : null,
            [ReminderTemplate.BranchName] = branch?.Name,
            [ReminderTemplate.BranchPhone] = branch?.Phone,
            [ReminderTemplate.DaysSinceWash] = row.LastWashAt is { } at ? DaysBetween(at, now).ToString(CultureInfo.InvariantCulture) : null,
            [ReminderTemplate.PackageName] = row.PackageName,
            [ReminderTemplate.RemainingUses] = row.RemainingUses?.ToString(CultureInfo.InvariantCulture),
            [ReminderTemplate.ExpiresOn] = row.ExpiresAt is { } exp ? VnTime.ToLocal(exp).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : null,
        });

    /// <summary>Calendar days in Vietnam time (washed yesterday evening = 1 day, whatever the hour).</summary>
    private static int DaysBetween(DateTime fromUtc, DateTime toUtc) =>
        DateOnly.FromDateTime(VnTime.ToLocal(toUtc)).DayNumber - DateOnly.FromDateTime(VnTime.ToLocal(fromUtc)).DayNumber;
}
