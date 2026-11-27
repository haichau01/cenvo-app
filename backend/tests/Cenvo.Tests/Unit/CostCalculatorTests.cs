using Cenvo.Modules.Inventory.Domain;

namespace Cenvo.Tests.Unit;

public class CostCalculatorTests
{
    [Fact]
    public void Moving_average_weights_by_quantity() =>
        Assert.Equal(275m, CostCalculator.MovingAverage(onHand: 1000, avgCost: 250, inQty: 1000, inCost: 300));

    [Fact]
    public void Empty_or_negative_stock_takes_incoming_cost()
    {
        Assert.Equal(300m, CostCalculator.MovingAverage(0, 250, 100, 300));
        Assert.Equal(300m, CostCalculator.MovingAverage(-50, 250, 100, 300));
    }

    [Fact]
    public void Zero_incoming_keeps_cost() => Assert.Equal(250m, CostCalculator.MovingAverage(10, 250, 0, 999));

    [Fact]
    public void Result_is_rounded_to_4_decimals() =>
        Assert.Equal(233.3333m, CostCalculator.MovingAverage(2, 200, 1, 300));
}
