using System.Net;

namespace Cenvo.Tests.Integration;

/// <summary>Regressions found while rewriting the read queries for readability.</summary>
[Collection(ApiCollection.Name)]
public class ReadModelTests(ApiFixture api)
{
    [Fact]
    public async Task Kitchen_cannot_move_an_item_back_two_steps()
    {
        var manager = await api.LoginAs("quanly1");
        var order = await manager.Post("/api/cafe/orders", new { type = "TakeAway" });
        await manager.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = await Seeded.ProductId(manager, "CF-SUA"), quantity = 1 });
        var sent = await manager.Post($"/api/cafe/orders/{order.Id()}/send");
        var lineId = sent["lines"]!.Arr()[0]!.Id();

        await manager.Put($"/api/cafe/kitchen/{lineId}", new { status = "Served" });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => manager.Put($"/api/cafe/kitchen/{lineId}", new { status = "Preparing" }));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task Sales_report_counts_orders_cancelled_before_payment()
    {
        var manager = await api.LoginAs("quanly1");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).ToString("yyyy-MM-dd");
        var report = $"/api/reports/cafe-sales?from={today}&to={today}";
        var before = (await manager.Get(report))["cancelled"]!.GetValue<int>();

        var order = await manager.Post("/api/cafe/orders", new { type = "TakeAway" });
        await manager.Post($"/api/cafe/orders/{order.Id()}/cancel", new { reason = "Khách đổi ý" });

        Assert.Equal(before + 1, (await manager.Get(report))["cancelled"]!.GetValue<int>());
    }

    [Fact]
    public async Task Low_stock_lists_only_items_below_their_minimum()
    {
        var manager = await api.LoginAs("quanly1");

        var rows = (await manager.Get("/api/inventory/low-stock")).Arr();

        Assert.All(rows, row => Assert.True(row!.Dec("quantity") < row.Dec("minStock")));
    }

    [Fact]
    public async Task Product_list_and_menu_use_the_same_selling_price()
    {
        var manager = await api.LoginAs("quanly1");

        var menu = (await manager.Get("/api/cafe/menu")).Arr();
        var products = (await manager.Get("/api/catalog/products")).Arr();

        foreach (var item in menu)
        {
            var product = products.FirstOrDefault(p => p!.Id() == item!.Id("productId"));
            if (product != null) Assert.Equal(product.Dec("price"), item!.Dec("price"));
        }
    }
}
