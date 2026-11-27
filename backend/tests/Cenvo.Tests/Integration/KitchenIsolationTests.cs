namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class KitchenIsolationTests(ApiFixture api)
{
    /// <summary>Regression: joining DiningTable with IgnoreQueryFilters dropped the branch filter on the kitchen queue.</summary>
    [Fact]
    public async Task Kitchen_queue_only_shows_own_branch_tickets()
    {
        var q1 = await api.LoginAs("quanly1");
        var q2 = await api.LoginAs("quanly2");
        var table = await Seeded.FirstFreeTable(q1);
        var order = await q1.Post("/api/cafe/orders", new { type = "DineIn", tableId = table.Id() });
        await q1.Post($"/api/cafe/orders/{order.Id()}/lines", new { productId = await Seeded.ProductId(q1, "CF-SUA"), quantity = 1 });
        await q1.Post($"/api/cafe/orders/{order.Id()}/send");

        var ownQueue = (await q1.Get("/api/cafe/kitchen")).Arr();
        var ticket = ownQueue.First(t => t!.Id("orderId") == order.Id())!;
        Assert.Equal(table.Str("name"), ticket.Str("tableName"));

        var otherQueue = (await q2.Get("/api/cafe/kitchen")).Arr();
        Assert.DoesNotContain(otherQueue, t => t!.Id("orderId") == order.Id());
    }
}
