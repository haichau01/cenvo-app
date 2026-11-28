namespace Cenvo.Modules.CarWash.Domain;

public record StaffCommission(Guid UserId, string FullName, decimal Amount);

/// <summary>
/// A vehicle finished washing → inventory deducts supplies (same transaction).
/// Carries the commission and timing so Payroll / Equipment can listen later without querying the ticket.
/// </summary>
public record WashTicketCompleted(
    Guid TicketId,
    Guid BranchId,
    string Code,
    VehicleType VehicleType,
    IReadOnlyList<Guid> OfferingIds,
    DateTime StartedAt,
    DateTime CompletedAt,
    IReadOnlyList<StaffCommission> Commissions) : IDomainEvent;

/// <param name="WasCompleted">True when supplies had been issued (and commission earned) — they must be reversed.</param>
public record WashTicketCancelled(Guid TicketId, string Code, string Reason, bool WasCompleted) : IDomainEvent;
