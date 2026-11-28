namespace Cenvo.Modules.CarWash.Application.Reports;

/// <remarks>Built in SQL → init properties.</remarks>
public record StaffCommissionDto
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required int TicketCount { get; init; }
    public required decimal Commission { get; init; }
}

public record CommissionReport(DateOnly From, DateOnly To, decimal Total, List<StaffCommissionDto> Staff);
