using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

/// <summary>Looks up seed data through the API (independent of internal C# types).</summary>
public static class Seeded
{
    public static async Task<Guid> ProductId(HttpClient c, string code)
    {
        var list = await c.Get($"/api/catalog/products?q={code}");
        return list.Arr().First(p => p!.Str("code") == code)!.Id();
    }

    public static async Task<Guid> UnitId(HttpClient c, string name) =>
        (await c.Get("/api/catalog/units")).Arr().First(u => u!.Str("name") == name)!.Id();

    public static async Task<Guid> DefaultWarehouseId(HttpClient c) =>
        (await c.Get("/api/inventory/warehouses")).Arr().First()!.Id();

    /// <summary>On-hand quantity (base unit) of a product in the client's branch default warehouse.</summary>
    public static async Task<decimal> OnHand(HttpClient c, string productCode)
    {
        var list = await c.Get($"/api/inventory/balances?q={productCode}");
        var row = list.Arr().FirstOrDefault(b => b!.Str("productCode") == productCode);
        return row?.Dec("quantity") ?? 0;
    }

    public static async Task<JsonNode> FirstFreeTable(HttpClient c)
    {
        var areas = await c.Get("/api/cafe/areas");
        return areas.Arr().SelectMany(a => a!["tables"]!.AsArray()).First(t => t!["openOrderId"] == null)!;
    }

    public static async Task EnsureShiftOpen(HttpClient c)
    {
        var current = await c.GetAsync("/api/cafe/shifts/current");
        var text = await current.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text) || text == "null") await c.Post("/api/cafe/shifts/open", new { openingCash = 500000 });
    }
}
