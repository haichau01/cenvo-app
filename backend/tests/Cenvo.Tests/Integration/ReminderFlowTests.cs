using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Cenvo.Modules.CarWash.Domain;
using Cenvo.Modules.CarWash.Domain.Calculations;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReminderFlowTests(ApiFixture api)
{
    private static string NewPlate() => $"51R-{Random.Shared.Next(10000, 99999)}";
    private static string NewPhone() => $"09{Random.Shared.Next(10000000, 99999999)}";

    private static async Task<JsonNode> WashFor(HttpClient c, string phone)
    {
        var service = (await c.Get("/api/carwash/services")).Arr().First(s => s!.Str("code") == "RX-BOT")!.Id();
        var staff = (await c.Get("/api/carwash/staff")).Arr().Select(s => s!.Id("userId")).ToArray();
        var ticket = await c.Post("/api/carwash/tickets", new
        {
            plate = NewPlate(), vehicleType = "Sedan", customerPhone = phone, customerName = "Chị Lan",
            services = new[] { new { offeringId = service, membershipId = (Guid?)null } }, staffIds = staff,
        });
        await c.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        return await c.Post($"/api/carwash/tickets/{ticket.Id()}/complete");
    }

    private Task Backdate(Guid customerId, int days) =>
        api.WithDb(db => db.Set<Customer>().Where(c => c.Id == customerId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastWashAt, DateTime.UtcNow.AddDays(-days))));

    private static async Task<JsonNode?> DueFor(HttpClient manager, Guid customerId, string kind) =>
        (await manager.Get("/api/carwash/reminders/due")).Arr()
        .FirstOrDefault(d => d!["reason"]!.Id("customerId") == customerId && d["reason"]!.Str("kind") == kind);

    private static Task<JsonNode> Send(HttpClient manager, string kind, params Guid[] refIds) =>
        manager.Post("/api/carwash/reminders/send", new { kind, refIds });

    [Fact]
    public async Task Return_reminder_is_due_after_the_set_days_sent_once_and_a_new_wash_starts_over()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var ticket = await WashFor(cashier, NewPhone());
        var customerId = ticket.Id("customerId");

        Assert.Null(await DueFor(manager, customerId, "ReturnVisit")); // washed just now
        await Backdate(customerId, 20);

        var due = await DueFor(manager, customerId, "ReturnVisit");
        Assert.NotNull(due);
        Assert.Equal(ticket.Id(), due["reason"]!.Id("refId"));
        Assert.Equal(ticket.Str("plate"), due["reason"]!.Str("plate"));
        Assert.Contains(PlateNumber.Format(ticket.Str("plate")), due.Str("message"));
        Assert.Contains("20 ngày", due.Str("message"));

        var result = await Send(manager, "ReturnVisit", ticket.Id());
        Assert.Equal(1, result["sent"]!.GetValue<int>());
        Assert.Null(await DueFor(manager, customerId, "ReturnVisit"));

        var again = await Send(manager, "ReturnVisit", ticket.Id());
        Assert.Equal(0, again["sent"]!.GetValue<int>());
        Assert.Equal(1, again["skipped"]!.GetValue<int>());

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).ToString("yyyy-MM-dd");
        var history = await manager.Get($"/api/carwash/reminders/history?from={today}&to={today}");
        Assert.Contains(history.Arr(), h => h!.Id("customerId") == customerId && h!.Str("status") == "Sent");
    }

    [Fact]
    public async Task Opted_out_customers_and_other_branches_are_not_reminded()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var otherBranchManager = await api.LoginAs("quanly2");
        var customerId = (await WashFor(cashier, NewPhone())).Id("customerId");
        await Backdate(customerId, 30);

        Assert.NotNull(await DueFor(manager, customerId, "ReturnVisit"));
        Assert.Null(await DueFor(otherBranchManager, customerId, "ReturnVisit"));

        await cashier.Put($"/api/carwash/customers/{customerId}/reminders", new { accepts = false });
        Assert.Null(await DueFor(manager, customerId, "ReturnVisit"));
        Assert.False((await cashier.Get($"/api/carwash/customers/{customerId}"))["acceptsReminders"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Cancelling_the_latest_wash_falls_back_to_the_previous_visit()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var phone = NewPhone();
        var first = await WashFor(cashier, phone);
        var customerId = first.Id("customerId");
        var firstId = first.Id();
        await api.WithDb(db => db.Set<WashTicket>().Where(t => t.Id == firstId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CompletedAt, DateTime.UtcNow.AddDays(-20))));

        var second = await WashFor(cashier, phone);
        Assert.Null(await DueFor(manager, customerId, "ReturnVisit")); // latest visit is today

        await manager.Post($"/api/carwash/tickets/{second.Id()}/cancel", new { reason = "Tạo nhầm phiếu" });
        var customer = await manager.Get($"/api/carwash/customers/{customerId}");
        Assert.NotNull(customer["lastWashAt"]);
        var due = await DueFor(manager, customerId, "ReturnVisit");
        Assert.NotNull(due);
        Assert.Equal(first.Id(), due["reason"]!.Id("refId"));
    }

    [Fact]
    public async Task Package_reminders_find_expiring_and_almost_used_up_packages()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        await Seeded.EnsureShiftOpen(cashier);
        var package = (await cashier.Get("/api/carwash/packages")).Arr().First(p => p!.Str("code") == "GOI-BOT-10")!;
        var membership = await cashier.Post("/api/carwash/memberships", new
        {
            packageId = package.Id(), customerPhone = NewPhone(), customerName = "Anh Hùng",
            payments = new[] { new { method = "Transfer", amount = package.Dec("price") } },
        });
        var id = membership.Id();
        var customerId = membership.Id("customerId");
        Assert.Null(await DueFor(manager, customerId, "MembershipExpiring"));

        await api.WithDb(db => db.Set<CustomerMembership>().Where(m => m.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(m => m.ExpiresAt, DateTime.UtcNow.AddDays(3))
            .SetProperty(m => m.RemainingUses, 1)));

        var expiring = await DueFor(manager, customerId, "MembershipExpiring");
        var lowUses = await DueFor(manager, customerId, "MembershipLowUses");
        Assert.NotNull(expiring);
        Assert.NotNull(lowUses);
        Assert.Equal(id, lowUses["reason"]!.Id("refId"));
        Assert.Contains("còn 1 lượt", lowUses.Str("message"));

        Assert.Equal(1, (await Send(manager, "MembershipLowUses", id))["sent"]!.GetValue<int>());
        Assert.Null(await DueFor(manager, customerId, "MembershipLowUses"));
        Assert.NotNull(await DueFor(manager, customerId, "MembershipExpiring")); // a different reason, still due
    }

    [Fact]
    public async Task Settings_are_validated_and_reminders_are_for_managers_only()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");

        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/carwash/reminders/due")).StatusCode);

        var settings = await manager.Get("/api/carwash/reminders/settings");
        var bad = await manager.PutAsJsonAsync("/api/carwash/reminders/settings", new
        {
            returnAfterDays = 14, expiringWithinDays = 7, lowUsesThreshold = 1,
            returnTemplate = "Chào {tenkhach}", expiringTemplate = settings.Str("expiringTemplate"),
            lowUsesTemplate = settings.Str("lowUsesTemplate"),
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("{tenkhach}", await bad.Content.ReadAsStringAsync());
    }
}
