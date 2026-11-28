using System.Text.RegularExpressions;

namespace Cenvo.Modules.CarWash.Domain.Calculations;

/// <summary>Licence plates are stored normalized (upper case, letters and digits only) so "51A-123.45" and "51a12345" match.</summary>
public static partial class PlateNumber
{
    public const int MinLength = 5;
    public const int MaxLength = 12;

    public static string Normalize(string? plate)
    {
        var normalized = NotAlphanumeric().Replace((plate ?? "").ToUpperInvariant(), "");
        if (normalized.Length is < MinLength or > MaxLength) throw new BusinessException(CarWashErrors.InvalidPlate);
        return normalized;
    }

    /// <summary>Plate fragment for "contains" searches: same normalization, any length (empty = no plate filter).</summary>
    public static string SearchPart(string? text) => NotAlphanumeric().Replace((text ?? "").ToUpperInvariant(), "");

    [GeneratedRegex("[^A-Z0-9]")]
    private static partial Regex NotAlphanumeric();
}
