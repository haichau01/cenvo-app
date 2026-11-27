using System.Net;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class BranchIsolationTests(ApiFixture api)
{
    [Fact]
    public async Task Branch_users_cannot_see_other_branch_orders_or_stock()
    {
        var q1 = await api.LoginAs("quanly1");
        var q2 = await api.LoginAs("quanly2");
        var order = await q1.Post("/api/cafe/orders", new { type = "TakeAway" });

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => q2.Get($"/api/cafe/orders/{order.Id()}"));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);

        var warehouses = (await q2.Get("/api/inventory/balances")).Arr()
            .Select(b => b!.Str("warehouseName")).Distinct().ToList();
        Assert.Equal(["Kho Thủ Đức"], warehouses);
    }

    [Fact]
    public async Task Cashier_cannot_access_inventory()
    {
        var cashier = await api.LoginAs("thungan1");
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => cashier.Get("/api/inventory/balances"));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }
}
