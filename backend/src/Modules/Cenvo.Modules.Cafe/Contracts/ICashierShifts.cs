namespace Cenvo.Modules.Cafe.Contracts;

/// <summary>The signed-in cashier's open shift — other sales sources (car wash) take payments inside it.</summary>
public interface ICashierShifts
{
    /// <summary>Id of the current user's open shift, or null when no shift is open.</summary>
    Task<Guid?> CurrentShiftId(CancellationToken ct);

    /// <summary>Name of the cashier who ran the shift (printed on receipts); null when the shift is not in the current branch.</summary>
    Task<string?> CashierName(Guid shiftId, CancellationToken ct);
}
