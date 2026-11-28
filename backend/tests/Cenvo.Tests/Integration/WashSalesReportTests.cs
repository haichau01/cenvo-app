using System.Net;
using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class WashSalesReportTests(ApiFixture api)
{
    private static string Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).ToString("yyyy-MM-dd");

    private static Task<JsonNode> Report(HttpClient c) => c.Get($"/api/reports/carwash-sales?from={Today}&to={Today}");

    private static async Task<JsonNode> PaidWash(HttpClient cashier)
    {
        var serviceId = (await cashier.Get("/api/carwash/services")).Arr().First(s => s!.Str("code") == "RX-BOT")!.Id();
        var washers = (await cashier.Get("/api/carwash/staff")).Arr().Select(s => s!.Id("userId")).ToArray();
        var ticket = await cashier.Post("/api/carwash/tickets", new
        {
            plate = $"51R-{Random.Shared.Next(10000, 99999)}", vehicleType = "Sedan",
            services = new[] { new { offeringId = serviceId, membershipId = (Guid?)null } }, staffIds = washers,
        });
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/complete");
        return await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/pay", new { payments = new[] { new { method = "Cash", amount = 100000 } } });
    }

    private static decimal Num(JsonNode report, string field) => report.Dec(field);

    [Fact]
    public async Task Report_adds_up_washes_packages_supplies_and_commission()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        await Seeded.EnsureShiftOpen(cashier);
        var before = await Report(manager);

        await PaidWash(cashier); // 100,000 VND, 20% commission, 150 ml foam + 30 ml shampoo issued
        var package = (await cashier.Get("/api/carwash/packages")).Arr().First(p => p!.Str("code") == "GOI-BOT-10")!;
        await cashier.Post("/api/carwash/memberships", new
        {
            packageId = package.Id(), customerPhone = $"09{Random.Shared.Next(10000000, 99999999)}", customerName = "Anh Hùng",
            payments = new[] { new { method = "Transfer", amount = package.Dec("price") } },
        });
        var after = await Report(manager);

        Assert.Equal(1, Num(after, "tickets") - Num(before, "tickets"));
        Assert.Equal(100000, Num(after, "washRevenue") - Num(before, "washRevenue"));
        Assert.Equal(package.Dec("price"), Num(after, "membershipRevenue") - Num(before, "membershipRevenue"));
        Assert.Equal(20000, Num(after, "commission") - Num(before, "commission"));
        Assert.True(Num(after, "supplyCost") > Num(before, "supplyCost"), "supplies issued by the wash must cost something");
        Assert.Equal(Num(after, "revenue") - Num(after, "supplyCost") - Num(after, "commission"), Num(after, "grossProfit"));
        Assert.Equal(100000, after["byMethod"]!["Cash"]!.GetValue<decimal>() - before["byMethod"]!["Cash"]!.GetValue<decimal>());

        var day = Assert.Single(after["daily"]!.AsArray())!;
        Assert.Equal(Today, day.Str("date"));
        Assert.Equal(Num(after, "revenue"), day.Dec("revenue"));
        Assert.Contains(after["byService"]!.AsArray(), s => s!.Dec("count") >= 1);
        Assert.Contains(after["byVehicle"]!.AsArray(), v => v!.Str("vehicleType") == "Sedan");
    }

    [Fact]
    public async Task Other_branch_sales_are_not_counted()
    {
        var cashier = await api.LoginAs("thungan1");
        var otherManager = await api.LoginAs("quanly2");
        await Seeded.EnsureShiftOpen(cashier);
        var before = await Report(otherManager);

        await PaidWash(cashier);

        Assert.Equal(Num(before, "washRevenue"), Num(await Report(otherManager), "washRevenue"));
    }

    [Fact]
    public async Task Cashier_cannot_open_the_report()
    {
        var cashier = await api.LoginAs("thungan1");
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Report(cashier));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }
}
