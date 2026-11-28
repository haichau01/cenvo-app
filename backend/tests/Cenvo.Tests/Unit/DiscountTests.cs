using Cenvo.Modules.Cafe.Domain;
using Cenvo.Modules.Cafe.Domain.Calculations;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class DiscountTests
{
    private static readonly DiscountApproval Manager = new(Guid.NewGuid(), "Quản lý");
    private static readonly Guid Cashier = Guid.NewGuid();

    /// <summary>Take-away bill of 100,000 VND: 4 × 25,000.</summary>
    private static CafeOrder Bill()
    {
        var order = CafeOrder.Open(Guid.NewGuid(), "HD-1", OrderType.TakeAway, null, null);
        order.AddLine(new MenuPrice(Guid.NewGuid(), "Cà phê đen", 25000), 4, [], null);
        return order;
    }

    [Theory]
    [InlineData(100000, 10000, false)]
    [InlineData(100000, 10001, true)]
    [InlineData(55555, 5555, false)] // limit rounds down to whole VND
    [InlineData(55555, 5556, true)]
    public void Cashier_limit_is_ten_percent(decimal gross, decimal discount, bool needsManager) =>
        Assert.Equal(needsManager, DiscountPolicy.NeedsManager(gross, discount));

    [Fact]
    public void Cashier_gives_up_to_the_limit_alone()
    {
        var order = Bill();
        order.ApplyDiscount(null, 10, "Khách quen", Cashier, "Thu", approval: null);

        Assert.Equal(90000, order.Total);
        Assert.Null(order.DiscountApprovedBy);
    }

    [Fact]
    public void Above_the_limit_needs_a_manager()
    {
        var order = Bill();
        Assert.Throws<BusinessException>(() => order.ApplyDiscount(null, 20, "Khách quen", Cashier, "Thu", approval: null));

        order.ApplyDiscount(null, 20, "Khách quen", Cashier, "Thu", Manager);
        Assert.Equal(80000, order.Total);
        Assert.Equal("Quản lý", order.DiscountApprovedByName);
    }

    [Fact]
    public void Item_and_order_discounts_count_together()
    {
        var order = Bill();
        order.DiscountLine(order.Lines[0].Id, 8000, "Món làm lỗi", Cashier, "Thu", approval: null);

        Assert.Throws<BusinessException>(() => order.ApplyDiscount(5000, null, "Khách quen", Cashier, "Thu", approval: null));
    }

    [Fact]
    public void An_approval_covers_a_smaller_discount_but_not_a_bigger_one()
    {
        var order = Bill();
        order.ApplyDiscount(30000, null, "Sinh nhật", Cashier, "Thu", Manager);

        order.ApplyDiscount(20000, null, "Sinh nhật", Cashier, "Thu", approval: null);
        Assert.Equal("Quản lý", order.DiscountApprovedByName);
        Assert.Throws<BusinessException>(() => order.ApplyDiscount(40000, null, "Sinh nhật", Cashier, "Thu", approval: null));
    }

    [Fact]
    public void A_discount_needs_a_reason() =>
        Assert.Throws<BusinessException>(() => Bill().ApplyDiscount(5000, null, " ", Cashier, "Thu", approval: null));

    [Fact]
    public void Removing_items_after_a_discount_blocks_payment_until_approved()
    {
        var order = Bill();
        order.AddLine(new MenuPrice(Guid.NewGuid(), "Bánh", 100000), 1, [], null); // gross 200,000
        order.ApplyDiscount(20000, null, "Khách quen", Cashier, "Thu", approval: null);  // exactly 10%
        order.RemoveLine(order.Lines[1].Id, byManager: false);                           // now 20% of 100,000

        Assert.Throws<BusinessException>(() => order.Pay(Guid.NewGuid(), Cashier, [new(PaymentMethod.Cash, 80000)], DateTime.UtcNow));
    }

    [Fact]
    public void A_percent_discount_follows_the_bill_when_items_are_split_off()
    {
        var order = Bill();                                                              // 4 × 25,000
        order.AddLine(new MenuPrice(Guid.NewGuid(), "Bánh", 100000), 1, [], null);       // 200,000
        order.ApplyDiscount(null, 20, "Khách quen", Cashier, "Thu", Manager);            // 40,000
        order.RemoveLine(order.Lines[1].Id, byManager: false);

        Assert.Equal(20000, order.DiscountAmount); // still 20%, not 40,000 on a 100,000 bill
    }

    [Fact]
    public void An_approved_amount_does_not_cover_a_bigger_share_after_items_are_removed()
    {
        var order = Bill();
        order.AddLine(new MenuPrice(Guid.NewGuid(), "Bánh", 100000), 1, [], null);       // 200,000
        order.ApplyDiscount(40000, null, "Khách quen", Cashier, "Thu", Manager);          // 20% approved
        order.RemoveLine(order.Lines[1].Id, byManager: false);                            // 40,000 of 100,000 = 40%

        Assert.Throws<BusinessException>(() => order.Pay(Guid.NewGuid(), Cashier, [new(PaymentMethod.Cash, 60000)], DateTime.UtcNow));
    }

    [Fact]
    public void An_approved_percent_still_covers_the_bill_after_items_are_removed()
    {
        var order = Bill();
        order.AddLine(new MenuPrice(Guid.NewGuid(), "Bánh", 100000), 1, [], null);
        order.ApplyDiscount(null, 20, "Khách quen", Cashier, "Thu", Manager);
        order.RemoveLine(order.Lines[1].Id, byManager: false);

        order.Pay(Guid.NewGuid(), Cashier, [new(PaymentMethod.Cash, 80000)], DateTime.UtcNow);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }
}
