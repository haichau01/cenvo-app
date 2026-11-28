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

    /// <summary>"51A12345" → "51A-123.45", "30K1234" → "30K-1234" (same rule as the frontend formatPlate). Unknown shapes as stored.</summary>
    public static string Format(string plate)
    {
        var match = CarPlate().Match(SearchPart(plate));
        if (!match.Success) return plate;
        var number = match.Groups[2].Value;
        var shown = number.Length == 5 ? $"{number[..3]}.{number[3..]}" : number;
        return $"{match.Groups[1].Value}-{shown}";
    }

    [GeneratedRegex("[^A-Z0-9]")]
    private static partial Regex NotAlphanumeric();

    // The series digit is lazy so a 5-digit number wins: 51A12345 → 51A + 12345, not 51A1 + 2345.
    [GeneratedRegex(@"^(\d{2}[A-Z]{1,2}\d??)(\d{4,5})$")]
    private static partial Regex CarPlate();
}
