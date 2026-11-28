namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <param name="MembershipId">Customer's membership that covers this service (one use), or null to pay the price.</param>
public record TicketServiceInput(Guid OfferingId, Guid? MembershipId);

public record CreateTicketInput(
    string Plate,
    VehicleType VehicleType,
    string? VehicleDescription,
    string? CustomerName,
    string? CustomerPhone,
    TicketServiceInput[] Services,
    Guid[]? StaffIds,
    string? Note);

public record AssignStaffInput(Guid[] StaffIds);

public record PayTicketInput(TenderedPayment[] Payments);

public record CancelTicketInput(string Reason);

public record TicketHistoryFilter(DateOnly From, DateOnly To, string? Q);
