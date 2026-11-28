namespace Cenvo.Modules.Attendance.Application.Holidays;

public record HolidayInput(DateOnly Date, string Name, decimal Multiplier);

public record HolidayDto
{
    public required Guid Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Name { get; init; }
    public required decimal Multiplier { get; init; }
}
