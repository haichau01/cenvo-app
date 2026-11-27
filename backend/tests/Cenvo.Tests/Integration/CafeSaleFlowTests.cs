using System.Net;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CafeSaleFlowTests(ApiFixture api)
{
    [Fact]
    public async Task Paying_order_issues_ingredients_by_recipe_and_cancelling_restores_them()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var before = await Seeded.OnHand(manager, "NL-CF-ROB");
        var cupsBefore = await Seeded.OnHand(manager, "BB-LY-500");

        await Seeded.EnsureShiftOpen(cashier);
        var table = await Seeded.FirstFreeTable(cashier);
        var order = await cashier.Post("/api/cafe/orders", new { type = "DineIn", tableId = table.Id() });
        var blackCoffee = await Seeded.ProductId(cashier, "CF-DEN"); // 20 g coffee + 1 cup per serving
        order = await cashier.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = blackCoffee, quantity = 2 });
        Assert.Equal(50000, order.Dec("total"));

        var paid = await cashier.Post($"/api/cafe/orders/{order.Id()}/pay",
            new { payments = new[] { new { method = "Cash", amount = 100000 } } });
        Assert.Equal("Paid", paid.Str("status"));
        Assert.Equal(50000, paid.Dec("changeAmount"));

        Assert.Equal(before - 40, await Seeded.OnHand(manager, "NL-CF-ROB"));
        Assert.Equal(cupsBefore - 2, await Seeded.OnHand(manager, "BB-LY-500"));

        await manager.Post($"/api/cafe/orders/{order.Id()}/cancel", new { reason = "Khách đổi ý" });
        Assert.Equal(before, await Seeded.OnHand(manager, "NL-CF-ROB"));
        Assert.Equal(cupsBefore, await Seeded.OnHand(manager, "BB-LY-500"));
    }

    [Fact]
    public async Task Toppings_are_issued_per_cup()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var before = await Seeded.OnHand(manager, "NL-TRAN-CHAU");

        await Seeded.EnsureShiftOpen(cashier);
        var order = await cashier.Post("/api/cafe/orders", new { type = "TakeAway" });
        var milkTea = await Seeded.ProductId(cashier, "TRA-SUA");      // 40 g tapioca pearls
        var extraPearl = await Seeded.ProductId(cashier, "TOP-TC");    // +30 g tapioca pearls, 7,000 VND
        order = await cashier.Post($"/api/cafe/orders/{order.Id()}/lines", new
        {
            productId = milkTea, quantity = 3, toppings = new[] { new { productId = extraPearl, quantity = 1 } },
        });
        Assert.Equal(3 * (35000 + 7000), order.Dec("total"));

        await cashier.Post($"/api/cafe/orders/{order.Id()}/pay",
            new { payments = new[] { new { method = "Transfer", amount = order.Dec("total") } } });
        Assert.Equal(before - 3 * (40 + 30), await Seeded.OnHand(manager, "NL-TRAN-CHAU"));
    }

    [Fact]
    public async Task Paying_without_open_shift_is_rejected()
    {
        var manager = await api.LoginAs("quanly1"); // the manager has not opened a shift
        var order = await manager.Post("/api/cafe/orders", new { type = "TakeAway" });
        var water = await Seeded.ProductId(manager, "NS-500");
        await manager.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = water, quantity = 1 });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => manager.Post($"/api/cafe/orders/{order.Id()}/pay",
            new { payments = new[] { new { method = "Cash", amount = 12000 } } }));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task Closing_shift_reconciles_cash()
    {
        var cashier = await api.LoginAs("thungan2");
        await cashier.Post("/api/cafe/shifts/open", new { openingCash = 300000 });
        var order = await cashier.Post("/api/cafe/orders", new { type = "TakeAway" });
        var water = await Seeded.ProductId(cashier, "NS-500");
        await cashier.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = water, quantity = 2 });
        await cashier.Post($"/api/cafe/orders/{order.Id()}/pay", new { payments = new[] { new { method = "Cash", amount = 50000 } } });

        var closed = await cashier.Post("/api/cafe/shifts/close", new { countedCash = 324000 });
        Assert.Equal(324000, closed.Dec("expectedCash"));
        Assert.Equal(0, closed.Dec("difference"));
    }
}
