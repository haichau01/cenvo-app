using Cenvo.Modules.CarWash.Application.Tickets;

namespace Cenvo.Modules.CarWash.Application.Customers;

public record CustomerInput(string? Name, string Phone, string? Note);

/// <remarks>Built in SQL → init properties.</remarks>
public record CustomerListItemDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Phone { get; init; }
    public required List<string> Plates { get; init; }
}

public record CustomerRef(Guid Id, string Name, string Phone);

/// <remarks>Built in SQL → init properties.</remarks>
public record VehicleDto
{
    public required Guid Id { get; init; }
    public required string Plate { get; init; }
    public required VehicleType VehicleType { get; init; }
    public required string? Description { get; init; }
}

/// <remarks>Built in SQL → init properties.</remarks>
public record MembershipDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required string PackageName { get; init; }
    public required Guid OfferingId { get; init; }
    public required string OfferingName { get; init; }
    public required VehicleType? VehicleType { get; init; }
    public required int TotalUses { get; init; }
    public required int RemainingUses { get; init; }
    public required decimal Price { get; init; }
    public required DateTime PaidAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required MembershipStatus Status { get; init; }
    /// <summary>Active, not expired and has uses left.</summary>
    public required bool IsUsable { get; init; }
}

public record CustomerDetailDto(
    Guid Id, string Name, string Phone, string? Note, bool AcceptsReminders, DateTime? LastWashAt,
    List<VehicleDto> Vehicles, List<MembershipDto> Memberships, List<TicketHistoryDto> RecentTickets);

/// <summary>What intake needs after typing a plate: the car, its owner, usable memberships, recent visits, a ticket still open.</summary>
public record VehicleLookupDto(
    VehicleDto Vehicle, CustomerRef? Customer, List<MembershipDto> Memberships, List<TicketHistoryDto> RecentTickets,
    TicketHistoryDto? OpenTicket);
