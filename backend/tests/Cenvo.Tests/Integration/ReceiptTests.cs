using System.Net;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReceiptTests(ApiFixture api)
{
    [Fact]
    public async Task Paid_order_receipt_has_shop_header_and_cashier()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var order = await cashier.Post("/api/cafe/orders", new { type = "TakeAway" });
        await cashier.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = await Seeded.ProductId(cashier, "CF-DEN"), quantity = 1 });
        await cashier.Post($"/api/cafe/orders/{order.Id()}/pay", new { payments = new[] { new { method = "Cash", amount = 50000 } } });

        var receipt = await cashier.Get($"/api/cafe/orders/{order.Id()}/receipt");

        Assert.Equal("Cenvo Quận 1", receipt["store"]!.Str("name"));
        Assert.Equal("12 Lê Lợi, Q.1, TP.HCM", receipt["store"]!.Str("address"));
        Assert.Equal("Trần Thị Thu", receipt.Str("cashierName"));
        Assert.Equal("Paid", receipt["order"]!.Str("status"));
        Assert.Equal(25000, receipt["order"]!.Dec("changeAmount"));
    }

    [Fact]
    public async Task Open_order_prints_as_pre_bill_without_cashier()
    {
        var manager = await api.LoginAs("quanly1");
        var order = await manager.Post("/api/cafe/orders", new { type = "TakeAway" });

        var receipt = await manager.Get($"/api/cafe/orders/{order.Id()}/receipt");

        Assert.Equal("Open", receipt["order"]!.Str("status"));
        Assert.Null(receipt["cashierName"]);
    }

    [Fact]
    public async Task Other_branch_cannot_print_the_receipt()
    {
        var q1 = await api.LoginAs("quanly1");
        var q2 = await api.LoginAs("quanly2");
        var order = await q1.Post("/api/cafe/orders", new { type = "TakeAway" });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => q2.Get($"/api/cafe/orders/{order.Id()}/receipt"));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
}
