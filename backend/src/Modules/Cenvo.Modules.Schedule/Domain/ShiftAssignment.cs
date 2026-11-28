namespace Cenvo.Modules.Schedule.Domain;

/// <summary>
/// One person on one shift on one day. The shift's name and hours are copied, so editing a template later does not
/// rewrite past schedules (attendance compares against them).
/// </summary>
public class ShiftAssignment : BaseEntity, IBranchScoped
{
    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateOnly Date { get; set; }
    public Guid ShiftTemplateId { get; set; }
    public string ShiftName { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    /// <summary>Length in minutes (weekly hours are summed in SQL).</summary>
    public int Minutes { get; set; }

    public ShiftWindow Window => new(StartsAt, EndsAt);

    public static ShiftAssignment Create(ShiftTemplate template, StaffMember employee, DateOnly date, DateOnly today)
    {
        if (!template.IsActive) throw new BusinessException(ScheduleErrors.TemplateInactive);
        EnsureNotPast(date, today);
        var window = ShiftWindow.On(date, template.StartTime, template.EndTime);
        return new ShiftAssignment
        {
            EmployeeId = employee.UserId, EmployeeName = employee.FullName, Date = date,
            ShiftTemplateId = template.Id, ShiftName = template.Name, StartTime = template.StartTime, EndTime = template.EndTime,
            StartsAt = window.StartUtc, EndsAt = window.EndUtc, Minutes = (int)(window.EndUtc - window.StartUtc).TotalMinutes,
        };
    }

    /// <summary>Days already gone are kept as they were planned.</summary>
    public static void EnsureNotPast(DateOnly date, DateOnly today)
    {
        if (date < today) throw new BusinessException(ScheduleErrors.PastDayLocked);
    }
}
