using System.Net;
using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class DiscountFlowTests(ApiFixture api)
{
    /// <summary>Take-away bill of 4 black coffees = 100,000 VND.</summary>
    private static async Task<JsonNode> Bill(HttpClient cashier)
    {
        var order = await cashier.Post("/api/cafe/orders", new { type = "TakeAway" });
        var coffee = await Seeded.ProductId(cashier, "CF-DEN");
        return await cashier.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = coffee, quantity = 4 });
    }

    [Fact]
    public async Task Cashier_needs_a_manager_account_above_ten_percent_and_the_shift_report_shows_it()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var order = await Bill(cashier);
        Assert.Equal(10000, order.Dec("cashierDiscountLimit"));

        var alone = await Assert.ThrowsAsync<HttpRequestException>(() =>
            cashier.Put($"/api/cafe/orders/{order.Id()}/discount", new { percent = 20, reason = "Khách quen" }));
        Assert.Equal(HttpStatusCode.BadRequest, alone.StatusCode);

        var otherBranchManager = await Assert.ThrowsAsync<HttpRequestException>(() => cashier.Put($"/api/cafe/orders/{order.Id()}/discount",
            new { percent = 20, reason = "Khách quen", approval = new { userName = "quanly2", password = "123456" } }));
        Assert.Equal(HttpStatusCode.BadRequest, otherBranchManager.StatusCode);

        order = await cashier.Put($"/api/cafe/orders/{order.Id()}/discount",
            new { percent = 20, reason = "Khách quen", approval = new { userName = "quanly1", password = "123456" } });
        Assert.Equal(80000, order.Dec("total"));
        Assert.Equal("Nguyễn Văn Quản", order.Str("discountApprovedByName"));

        await cashier.Post($"/api/cafe/orders/{order.Id()}/pay", new { payments = new[] { new { method = "Cash", amount = 80000 } } });
        var shift = await cashier.Get("/api/cafe/shifts/current");
        var discount = shift["discounts"]!.AsArray().First(d => d!.Str("orderCode") == order.Str("code"))!;
        Assert.Equal(20000, discount.Dec("amount"));
        Assert.Equal("Khách quen", discount.Str("reasons"));
        Assert.Equal("Nguyễn Văn Quản", discount.Str("approvedByName"));
    }

    [Fact]
    public async Task Item_discount_needs_a_reason_and_stays_within_the_cashier_limit()
    {
        var cashier = await api.LoginAs("thungan1");
        var order = await Bill(cashier);
        var lineId = order["lines"]![0]!.Id();

        var noReason = await Assert.ThrowsAsync<HttpRequestException>(() =>
            cashier.Put($"/api/cafe/orders/{order.Id()}/lines/{lineId}/discount", new { amount = 5000 }));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        order = await cashier.Put($"/api/cafe/orders/{order.Id()}/lines/{lineId}/discount", new { amount = 5000, reason = "Món làm lỗi" });
        Assert.Equal(95000, order.Dec("total"));
        Assert.Equal("Món làm lỗi", order["lines"]![0]!.Str("discountReason"));
    }
}
