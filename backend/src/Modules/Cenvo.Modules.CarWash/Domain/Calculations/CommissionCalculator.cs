namespace Cenvo.Modules.CarWash.Domain.Calculations;

/// <summary>Car wash commission. Pure functions for unit testing; amounts are rounded to whole VND.</summary>
public static class CommissionCalculator
{
    /// <summary>Commission earned by one service line (before splitting between staff).</summary>
    public static decimal ForService(decimal price, CommissionType type, decimal value)
    {
        if (value <= 0) return 0;
        return type == CommissionType.Percent
            ? Math.Round(price * value / 100, 0, MidpointRounding.AwayFromZero)
            : Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Splits the total equally. Shares are whole VND and add up exactly to the total:
    /// the remainder (less than one VND per person) goes to the first people.
    /// </summary>
    public static IReadOnlyList<decimal> SplitEqually(decimal total, int people)
    {
        if (people <= 0) return [];
        var wholeTotal = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        var share = Math.Floor(wholeTotal / people);
        var remainder = (int)(wholeTotal - share * people);
        return Enumerable.Range(0, people).Select(i => share + (i < remainder ? 1 : 0)).ToList();
    }
}
