namespace Cenvo.Modules.Cafe.Contracts;

public record ShiftTakingsPayment(PaymentMethod Method, decimal Amount);

/// <summary>Money another module took inside a shift (e.g. car wash tickets, membership sales).</summary>
public record ShiftTakings(string Source, int Count, decimal Revenue, IReadOnlyList<ShiftTakingsPayment> Payments);

/// <summary>
/// Lets modules that depend on Cafe add their takings to the shift reconciliation without Cafe knowing them.
/// Cancelled (refunded) documents must be excluded.
/// </summary>
public interface IShiftTakingsSource
{
    Task<IReadOnlyList<ShiftTakings>> ForShift(Guid shiftId, CancellationToken ct);
}
