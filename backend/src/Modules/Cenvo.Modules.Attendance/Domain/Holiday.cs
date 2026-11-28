using Microsoft.EntityFrameworkCore;

namespace Cenvo.Modules.Attendance.Domain;

/// <summary>
/// A public holiday for the whole chain. Working it counts <see cref="Multiplier"/> days (and overtime × multiplier);
/// not working it still counts one paid day.
/// </summary>
public class Holiday : BaseEntity, ISoftDelete
{
    public const decimal DefaultMultiplier = 3m;

    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
    [Precision(4, 2)] public decimal Multiplier { get; set; } = DefaultMultiplier;
    public bool IsDeleted { get; set; }

    public void Update(DateOnly date, string? name, decimal multiplier)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException(AttendanceErrors.HolidayNameRequired);
        if (multiplier is < 1 or > 5) throw new BusinessException(AttendanceErrors.InvalidMultiplier);
        Date = date;
        Name = name.Trim();
        Multiplier = Math.Round(multiplier, 2);
    }
}
