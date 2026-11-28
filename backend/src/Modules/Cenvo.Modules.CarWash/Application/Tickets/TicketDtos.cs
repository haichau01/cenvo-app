namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <summary>Card on the wash queue (Kanban).</summary>
/// <remarks>Built in SQL → init properties (expression trees cannot use named arguments).</remarks>
public record QueueTicketDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Plate { get; init; }
    public required VehicleType VehicleType { get; init; }
    public required string? VehicleDescription { get; init; }
    public required string? CustomerName { get; init; }
    public required WashStatus Status { get; init; }
    public required bool IsPaid { get; init; }
    public required decimal Total { get; init; }
    /// <summary>Sum of the services' usual durations.</summary>
    public required int EstimatedMinutes { get; init; }
    public required List<string> Services { get; init; }
    public required List<string> Staff { get; init; }
    public required int PhotoCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? StartedAt { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public required DateTime? DeliveredAt { get; init; }
}

public record TicketLineDto(Guid Id, Guid OfferingId, string Name, decimal Price, decimal Amount, Guid? MembershipId,
    int DurationMinutes);

public record TicketStaffDto(Guid UserId, string FullName, decimal Commission);

public record TicketPaymentDto(PaymentMethod Method, decimal Amount, string? Reference);

public record TicketPhotoDto(Guid Id, DateTime TakenAt);

/// <remarks>Built in SQL → init properties.</remarks>
public record TicketDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid VehicleId { get; init; }
    public required string Plate { get; init; }
    public required VehicleType VehicleType { get; init; }
    public required string? VehicleDescription { get; init; }
    public required Guid? CustomerId { get; init; }
    public required string? CustomerName { get; init; }
    public required string? CustomerPhone { get; init; }
    public required string? Note { get; init; }
    public required WashStatus Status { get; init; }
    public required bool IsPaid { get; init; }
    public required bool IsEditable { get; init; }
    public required bool CanAssignStaff { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal MembershipCovered { get; init; }
    public required decimal Total { get; init; }
    public required decimal CashReceived { get; init; }
    public required decimal ChangeAmount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? StartedAt { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public required DateTime? DeliveredAt { get; init; }
    public required DateTime? PaidAt { get; init; }
    public required DateTime? CancelledAt { get; init; }
    public required string? CancelReason { get; init; }
    public required List<TicketLineDto> Lines { get; init; }
    public required List<TicketStaffDto> Staff { get; init; }
    public required List<TicketPaymentDto> Payments { get; init; }
    public required List<TicketPhotoDto> Photos { get; init; }
}

/// <summary>Row of the ticket history (also shown on the customer / vehicle page).</summary>
/// <remarks>Built in SQL → init properties.</remarks>
public record TicketHistoryDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Plate { get; init; }
    public required string? CustomerName { get; init; }
    public required WashStatus Status { get; init; }
    public required bool IsPaid { get; init; }
    public required decimal Total { get; init; }
    public required List<string> Services { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record WasherDto(Guid UserId, string FullName);
