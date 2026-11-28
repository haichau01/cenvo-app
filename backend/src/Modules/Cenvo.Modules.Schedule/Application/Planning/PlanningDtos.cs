using Cenvo.Modules.Schedule.Application.Templates;

namespace Cenvo.Modules.Schedule.Application.Planning;

public record AssignInput(DateOnly Date, Guid ShiftTemplateId, Guid EmployeeId);

/// <param name="WeeklyHours">The employee's hours that week in every branch, after this assignment.</param>
/// <param name="OverLimit">Above the branch's weekly limit — allowed, but the manager should know.</param>
public record AssignResult(Guid Id, decimal WeeklyHours, bool OverLimit);

/// <remarks>Built in SQL → init properties.</remarks>
public record AssignmentDto
{
    public required Guid Id { get; init; }
    public required DateOnly Date { get; init; }
    public required Guid ShiftTemplateId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
}

public record WeekStaffDto(Guid UserId, string FullName, Department? Department, decimal WeeklyHours, bool OverLimit);

public record ShortfallDto(DateOnly Date, Guid ShiftTemplateId, Department Department, int Required, int Assigned);

public record WeekDto(
    DateOnly WeekStart, int MaxWeeklyHours, List<TemplateDto> Templates, List<AssignmentDto> Assignments,
    List<WeekStaffDto> Staff, List<ShortfallDto> Shortfalls);

/// <remarks>Built in SQL → init properties (branch name filled in afterwards).</remarks>
public record MyShiftDto
{
    public required Guid Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string ShiftName { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public required Guid BranchId { get; init; }
    public string BranchName { get; init; } = "";
}
