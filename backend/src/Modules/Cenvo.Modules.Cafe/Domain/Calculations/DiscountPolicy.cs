namespace Cenvo.Modules.Cafe.Domain.Calculations;

/// <summary>Who may discount how much. Pure function for unit testing.</summary>
public static class DiscountPolicy
{
    /// <summary>A cashier may discount up to this share of the bill alone; above it a manager must approve.</summary>
    public const decimal CashierMaxPercent = 10;

    /// <summary>Slack for whole-VND rounding of a percent discount when the bill changes after approval.</summary>
    private const decimal RoundingSlack = 1;

    /// <summary>Largest total discount (order + items) a cashier may give alone, in whole VND.</summary>
    public static decimal CashierLimit(decimal grossAmount) =>
        Math.Floor(grossAmount * CashierMaxPercent / 100);

    /// <summary>Percentage of an amount in whole VND (half rounds up, like the POS preview).</summary>
    public static decimal PercentOf(decimal amount, decimal percent) =>
        Math.Round(amount * percent / 100, 0, MidpointRounding.AwayFromZero);

    public static bool NeedsManager(decimal grossAmount, decimal totalDiscount) =>
        totalDiscount > CashierLimit(grossAmount);

    /// <summary>
    /// An approval covers the share of the bill the manager saw (discount ÷ gross), not a fixed amount:
    /// removing items after approval must not turn it into a bigger share. Compared by cross-multiplying.
    /// </summary>
    public static bool WithinApproved(decimal grossAmount, decimal totalDiscount, decimal approvedGross, decimal approvedDiscount) =>
        (totalDiscount - RoundingSlack) * approvedGross <= approvedDiscount * grossAmount;
}

/// <summary>The manager who approved a discount above the cashier's limit (themselves when a manager applies it).</summary>
public record DiscountApproval(Guid ManagerId, string ManagerName);
