namespace Cenvo.Modules.Schedule.Application.Templates;

public record StaffingNeedDto(Department Department, int MinStaff);

public record TemplateInput(string Name, TimeOnly StartTime, TimeOnly EndTime, int SortOrder, bool IsActive, List<StaffingNeedDto> Needs);

public record TemplateDto(
    Guid Id, string Name, TimeOnly StartTime, TimeOnly EndTime, bool Overnight, int SortOrder, bool IsActive, List<StaffingNeedDto> Needs);

public record ScheduleSettingsDto(int MaxWeeklyHours);
