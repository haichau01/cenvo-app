namespace Cenvo.Modules.Schedule.Domain;

/// <summary>Scheduling limits of a branch; defaults until a manager saves them.</summary>
public class ScheduleSettings : BaseEntity, IBranchScoped
{
    /// <summary>The Labour Code cap on normal + overtime hours per week; above it the grid warns.</summary>
    public const int DefaultMaxWeeklyHours = 48;

    public Guid BranchId { get; set; }
    public int MaxWeeklyHours { get; set; } = DefaultMaxWeeklyHours;

    public void Update(int maxWeeklyHours)
    {
        if (maxWeeklyHours is < 1 or > 168) throw new BusinessException(ScheduleErrors.InvalidMaxWeeklyHours);
        MaxWeeklyHours = maxWeeklyHours;
    }
}
