using System.Text.RegularExpressions;

namespace Cenvo.Modules.CarWash.Domain.Calculations;

/// <summary>
/// Reminder message templates with Vietnamese placeholders, e.g. "Chào {ten}, xe {bienso} đã {songay} ngày chưa rửa…".
/// Pure functions → unit tested.
/// </summary>
public static partial class ReminderTemplate
{
    /// <summary>Two SMS segments without accents; Zalo allows more, but a reminder should stay short.</summary>
    public const int MaxLength = 320;
    /// <summary>Filled-in message cap (long names or branch details): cut rather than fail the send.</summary>
    public const int MaxMessageLength = 640;

    public const string CustomerName = "ten";
    public const string Plate = "bienso";
    public const string BranchName = "chinhanh";
    public const string BranchPhone = "sdtchinhanh";
    public const string DaysSinceWash = "songay";
    public const string PackageName = "goi";
    public const string RemainingUses = "conlai";
    public const string ExpiresOn = "hethan";

    public static readonly IReadOnlyList<string> Placeholders =
        [CustomerName, Plate, BranchName, BranchPhone, DaysSinceWash, PackageName, RemainingUses, ExpiresOn];

    /// <summary>Replaces every known placeholder; a missing value becomes an empty string. Unknown ones stay as typed.</summary>
    public static string Render(string template, IReadOnlyDictionary<string, string?> values)
    {
        var text = Placeholder().Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return Placeholders.Contains(key) ? values.GetValueOrDefault(key) ?? "" : m.Value;
        }).Trim();
        return text.Length > MaxMessageLength ? text[..MaxMessageLength] : text;
    }

    /// <summary>Placeholders written in the template that the system does not know (likely typos).</summary>
    public static IReadOnlyList<string> UnknownPlaceholders(string template) =>
        Placeholder().Matches(template).Select(m => m.Groups[1].Value).Where(k => !Placeholders.Contains(k)).Distinct().ToList();

    [GeneratedRegex(@"\{([a-z]+)\}")]
    private static partial Regex Placeholder();
}
