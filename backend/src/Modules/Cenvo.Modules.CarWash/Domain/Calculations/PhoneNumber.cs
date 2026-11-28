using System.Text.RegularExpressions;

namespace Cenvo.Modules.CarWash.Domain.Calculations;

/// <summary>Phone numbers are stored as digits only; "+84" becomes a leading 0.</summary>
public static partial class PhoneNumber
{
    /// <returns>Digits only, or null when nothing was entered.</returns>
    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = NotDigit().Replace(phone, "");
        if (phone.TrimStart().StartsWith('+') && digits.StartsWith("84")) digits = "0" + digits[2..];
        if (digits.Length is < 9 or > 11) throw new BusinessException(CarWashErrors.InvalidPhone);
        return digits;
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex NotDigit();
}
