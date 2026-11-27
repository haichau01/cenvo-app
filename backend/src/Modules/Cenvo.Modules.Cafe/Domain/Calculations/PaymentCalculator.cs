namespace Cenvo.Modules.Cafe.Domain.Calculations;

public record TenderedPayment(PaymentMethod Method, decimal Amount, string? Reference = null);

/// <param name="Recorded">Payments recorded on the order (cash = amount actually kept, change already deducted).</param>
public record PaymentResult(IReadOnlyList<TenderedPayment> Recorded, decimal CashReceived, decimal Change);

/// <summary>Splits the customer's tender into payments + change. Pure function for unit testing.</summary>
public static class PaymentCalculator
{
    public static PaymentResult Calculate(decimal total, IReadOnlyCollection<TenderedPayment> tendered)
    {
        if (tendered.Any(p => p.Amount < 0)) throw new BusinessException(CafeErrors.InvalidAmount);

        var nonCash = tendered.Where(p => p.Method != PaymentMethod.Cash && p.Amount > 0).ToList();
        var nonCashTotal = nonCash.Sum(p => p.Amount);
        if (nonCashTotal > total) throw new BusinessException(CafeErrors.NonCashExceedsTotal);

        var cashDue = total - nonCashTotal;
        var cashGiven = tendered.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount);
        if (cashGiven < cashDue) throw new BusinessException(CafeErrors.CustomerOwes(cashDue - cashGiven));

        var recorded = cashDue > 0 ? nonCash.Append(new TenderedPayment(PaymentMethod.Cash, cashDue)).ToList() : nonCash;
        return new PaymentResult(recorded, cashGiven, cashGiven - cashDue);
    }
}
