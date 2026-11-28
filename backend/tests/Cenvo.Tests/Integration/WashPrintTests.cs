using System.Net;
using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class WashPrintTests(ApiFixture api)
{
    private static async Task<JsonNode> Intake(HttpClient c, string? phone = null)
    {
        var serviceId = (await c.Get("/api/carwash/services")).Arr().First(s => s!.Str("code") == "RX-THUONG")!.Id();
        return await c.Post("/api/carwash/tickets", new
        {
            plate = $"51P-{Random.Shared.Next(10000, 99999)}", vehicleType = "Sedan",
            customerPhone = phone, customerName = phone == null ? null : "Chị Lan",
            services = new[] { new { offeringId = serviceId, membershipId = (Guid?)null } },
            staffIds = Array.Empty<Guid>(),
        });
    }

    private static string NewPhone() => $"09{Random.Shared.Next(10000000, 99999999)}";

    [Fact]
    public async Task Intake_slip_has_shop_header_and_no_cashier_before_payment()
    {
        var washer = await api.LoginAs("ruaxe1");
        var ticket = await Intake(washer);

        var slip = await washer.Get($"/api/carwash/tickets/{ticket.Id()}/receipt");

        Assert.Equal("Cenvo Quận 1", slip["store"]!.Str("name"));
        Assert.Equal(ticket.Str("code"), slip["ticket"]!.Str("code"));
        Assert.Null(slip["cashierName"]);
    }

    [Fact]
    public async Task Paid_ticket_receipt_names_the_cashier_and_change()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var ticket = await Intake(cashier); // 60,000 VND
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/pay", new { payments = new[] { new { method = "Cash", amount = 100000 } } });

        var receipt = await cashier.Get($"/api/carwash/tickets/{ticket.Id()}/receipt");

        Assert.Equal("Trần Thị Thu", receipt.Str("cashierName"));
        Assert.Equal(40000, receipt["ticket"]!.Dec("changeAmount"));
    }

    [Fact]
    public async Task Other_branch_cannot_print_a_ticket()
    {
        var q1 = await api.LoginAs("quanly1");
        var q2 = await api.LoginAs("quanly2");
        var ticket = await Intake(q1);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => q2.Get($"/api/carwash/tickets/{ticket.Id()}/receipt"));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task Membership_receipt_prints_at_the_selling_branch_only()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var package = (await cashier.Get("/api/carwash/packages")).Arr().First(p => p!.Str("code") == "GOI-BOT-10")!;
        var phone = NewPhone();
        var membership = await cashier.Post("/api/carwash/memberships", new
        {
            packageId = package.Id(), customerPhone = phone, customerName = "Chị Lan",
            payments = new[] { new { method = "Transfer", amount = 1000000 } },
        });

        var receipt = await cashier.Get($"/api/carwash/memberships/{membership.Id()}/receipt");
        Assert.Equal("Chị Lan", receipt.Str("customerName"));
        Assert.Equal(phone, receipt.Str("customerPhone"));
        Assert.Equal("Transfer", receipt["payments"]!.AsArray()[0]!.Str("method"));
        Assert.Equal("Trần Thị Thu", receipt.Str("cashierName"));

        var otherBranch = await api.LoginAs("quanly2");
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            otherBranch.Get($"/api/carwash/memberships/{membership.Id()}/receipt"));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
}
