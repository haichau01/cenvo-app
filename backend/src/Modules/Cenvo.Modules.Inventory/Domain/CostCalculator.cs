namespace Cenvo.Modules.Inventory.Domain;

/// <summary>Moving weighted-average cost — pure function for unit testing.</summary>
public static class CostCalculator
{
    public static decimal MovingAverage(decimal onHand, decimal avgCost, decimal inQty, decimal inCost)
    {
        if (inQty <= 0) return avgCost;
        if (onHand <= 0) return inCost; // out of stock or negative: new cost = receipt cost
        return Math.Round((onHand * avgCost + inQty * inCost) / (onHand + inQty), 4);
    }
}
