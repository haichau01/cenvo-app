using System.Net;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class InventoryFlowTests(ApiFixture api)
{
    [Fact]
    public async Task Purchase_receipt_updates_moving_average_cost()
    {
        var keeper = await api.LoginAs("thukho1");
        var sugar = await Seeded.ProductId(keeper, "NL-DUONG");
        var kg = await Seeded.UnitId(keeper, "kg");
        var wh = await Seeded.DefaultWarehouseId(keeper);
        var before = await Seeded.OnHand(keeper, "NL-DUONG");

        var id = await keeper.Post("/api/inventory/documents", new
        {
            type = "PurchaseReceipt", warehouseId = wh,
            lines = new[] { new { productId = sugar, unitId = kg, quantity = 2, unitPrice = 30000 } },
        });
        await keeper.Post($"/api/inventory/documents/{id.GetValue<Guid>()}/post");

        Assert.Equal(before + 2000, await Seeded.OnHand(keeper, "NL-DUONG"));
        var doc = await keeper.Get($"/api/inventory/documents/{id.GetValue<Guid>()}");
        Assert.Equal("Posted", doc.Str("status"));
        Assert.Equal(60000, doc.Dec("totalAmount"));
    }

    [Fact]
    public async Task Transfer_between_branches_moves_stock_after_receiving()
    {
        var from = await api.LoginAs("quanly1");
        var to = await api.LoginAs("quanly2");
        var arabica = await Seeded.ProductId(from, "NL-CF-ARA");
        var kg = await Seeded.UnitId(from, "kg");
        var fromWh = await Seeded.DefaultWarehouseId(from);
        var toWh = await Seeded.DefaultWarehouseId(to);
        var fromBefore = await Seeded.OnHand(from, "NL-CF-ARA");
        var toBefore = await Seeded.OnHand(to, "NL-CF-ARA");

        var id = (await from.Post("/api/inventory/documents", new
        {
            type = "Transfer", warehouseId = fromWh, toWarehouseId = toWh,
            lines = new[] { new { productId = arabica, unitId = kg, quantity = 1, unitPrice = 0 } },
        })).GetValue<Guid>();
        await from.Post($"/api/inventory/documents/{id}/post");
        Assert.Equal(fromBefore - 1000, await Seeded.OnHand(from, "NL-CF-ARA"));
        Assert.Equal(toBefore, await Seeded.OnHand(to, "NL-CF-ARA"));

        var incoming = await to.Get("/api/inventory/documents/incoming");
        Assert.Contains(incoming.Arr(), d => d!.Id() == id);
        await to.Post($"/api/inventory/documents/{id}/receive");
        Assert.Equal(toBefore + 1000, await Seeded.OnHand(to, "NL-CF-ARA"));
    }

    [Fact]
    public async Task Stocktake_adjusts_balance_to_counted_quantity()
    {
        var keeper = await api.LoginAs("thukho1");
        var syrup = await Seeded.ProductId(keeper, "NL-SYRUP-DAO");
        var wh = await Seeded.DefaultWarehouseId(keeper);

        var id = (await keeper.Post("/api/inventory/documents", new
        {
            type = "Stocktake", warehouseId = wh,
            lines = new[] { new { productId = syrup, unitId = (Guid?)null, quantity = 1234, unitPrice = 0 } },
        })).GetValue<Guid>();
        await keeper.Post($"/api/inventory/documents/{id}/post");

        Assert.Equal(1234, await Seeded.OnHand(keeper, "NL-SYRUP-DAO"));
    }

    [Fact]
    public async Task Cancelling_receipt_reverses_stock()
    {
        var keeper = await api.LoginAs("thukho1");
        var manager = await api.LoginAs("quanly1");
        var milk = await Seeded.ProductId(keeper, "NL-SUA-TUOI");
        var wh = await Seeded.DefaultWarehouseId(keeper);
        var before = await Seeded.OnHand(keeper, "NL-SUA-TUOI");

        var id = (await keeper.Post("/api/inventory/documents", new
        {
            type = "PurchaseReceipt", warehouseId = wh,
            lines = new[] { new { productId = milk, unitId = (Guid?)null, quantity = 500, unitPrice = 40 } },
        })).GetValue<Guid>();
        await keeper.Post($"/api/inventory/documents/{id}/post");
        await manager.Post($"/api/inventory/documents/{id}/cancel", new { reason = "Nhập nhầm" });

        Assert.Equal(before, await Seeded.OnHand(keeper, "NL-SUA-TUOI"));
    }

    [Fact]
    public async Task Issuing_more_than_on_hand_is_rejected_when_negative_stock_disallowed()
    {
        var keeper = await api.LoginAs("thukho1");
        var cream = await Seeded.ProductId(keeper, "NL-KEM-BEO");
        var wh = await Seeded.DefaultWarehouseId(keeper);

        var id = (await keeper.Post("/api/inventory/documents", new
        {
            type = "DisposalIssue", warehouseId = wh,
            lines = new[] { new { productId = cream, unitId = (Guid?)null, quantity = 999999, unitPrice = 0 } },
        })).GetValue<Guid>();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => keeper.Post($"/api/inventory/documents/{id}/post"));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }
}
