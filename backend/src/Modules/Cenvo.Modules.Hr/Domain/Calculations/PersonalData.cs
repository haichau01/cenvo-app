namespace Cenvo.Modules.Hr.Domain.Calculations;

/// <summary>Normalizes personal identifiers typed by staff. Empty input = not given (null).</summary>
public static class PersonalData
{
    public const int NationalIdLength = 12;

    public static string? NationalId(string? text)
    {
        var digits = Digits(text);
        if (digits == null) return null;
        if (digits.Length != NationalIdLength || digits.Length != text!.Trim().Replace(" ", "").Length)
            throw new BusinessException(HrErrors.InvalidNationalId);
        return digits;
    }

    /// <summary>"0901 234 567", "+84 901234567" → "0901234567".</summary>
    public static string? Phone(string? text)
    {
        var digits = Digits(text);
        if (digits == null) return null;
        if (digits.StartsWith("84") && digits.Length == 11) digits = "0" + digits[2..];
        if (digits.Length is < 9 or > 11) throw new BusinessException(HrErrors.InvalidPhone);
        return digits;
    }

    private static string? Digits(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return new string(text.Where(char.IsDigit).ToArray());
    }
}
