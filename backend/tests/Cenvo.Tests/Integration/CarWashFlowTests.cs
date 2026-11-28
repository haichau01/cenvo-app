using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CarWashFlowTests(ApiFixture api)
{
    /// <summary>Unique plate per test run: a vehicle can only have one ticket in progress.</summary>
    private static string NewPlate() => $"51T-{Random.Shared.Next(10000, 99999)}";

    private static async Task<Guid> ServiceId(HttpClient c, string code) =>
        (await c.Get("/api/carwash/services")).Arr().First(s => s!.Str("code") == code)!.Id();

    private static async Task<Guid[]> WasherIds(HttpClient c) =>
        (await c.Get("/api/carwash/staff")).Arr().Select(s => s!.Id("userId")).ToArray();

    private static async Task<JsonNode> Intake(HttpClient c, string serviceCode, Guid? membershipId = null, string? phone = null,
        bool withStaff = true) =>
        await c.Post("/api/carwash/tickets", new
        {
            plate = NewPlate(), vehicleType = "Sedan", customerPhone = phone, customerName = phone == null ? null : "Anh Tâm",
            services = new[] { new { offeringId = await ServiceId(c, serviceCode), membershipId } },
            staffIds = withStaff ? await WasherIds(c) : [],
        });

    private static async Task<JsonNode> BuyMembership(HttpClient c, string phone)
    {
        var package = (await c.Get("/api/carwash/packages")).Arr().First(p => p!.Str("code") == "GOI-BOT-10")!;
        return await c.Post("/api/carwash/memberships", new
        {
            packageId = package.Id(), customerPhone = phone, customerName = "Anh Tâm",
            payments = new[] { new { method = "Transfer", amount = 1000000 } },
        });
    }

    private static async Task<decimal> ShiftCash(HttpClient cashier) =>
        (await cashier.Get("/api/cafe/shifts/current"))["byMethod"]!["Cash"]!.GetValue<decimal>();

    [Fact]
    public async Task Completing_a_wash_issues_supplies_splits_commission_and_cancelling_returns_supplies()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var foamBefore = await Seeded.OnHand(manager, "HC-BOT");
        var washBefore = await Seeded.OnHand(manager, "HC-RUA");

        var ticket = await Intake(cashier, "RX-BOT"); // 100,000 VND for a sedan, 20% commission, 150 ml foam + 30 ml shampoo
        Assert.Equal(100000, ticket.Dec("total"));
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        ticket = await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/complete");

        Assert.Equal("Done", ticket.Str("status"));
        Assert.All(ticket["staff"]!.AsArray(), s => Assert.Equal(10000, s!.Dec("commission")));
        Assert.Equal(foamBefore - 150, await Seeded.OnHand(manager, "HC-BOT"));
        Assert.Equal(washBefore - 30, await Seeded.OnHand(manager, "HC-RUA"));

        await manager.Post($"/api/carwash/tickets/{ticket.Id()}/cancel", new { reason = "Rửa nhầm xe" });
        Assert.Equal(foamBefore, await Seeded.OnHand(manager, "HC-BOT"));
        Assert.Equal(washBefore, await Seeded.OnHand(manager, "HC-RUA"));
    }

    [Fact]
    public async Task Wash_payment_goes_into_the_cashier_shift_and_the_car_can_then_be_delivered()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var cashBefore = await ShiftCash(cashier);

        var ticket = await Intake(cashier, "RX-THUONG"); // 60,000 VND
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/complete");
        var notPaid = await Assert.ThrowsAsync<HttpRequestException>(() => cashier.Post($"/api/carwash/tickets/{ticket.Id()}/deliver"));
        Assert.Equal(HttpStatusCode.BadRequest, notPaid.StatusCode);

        var paid = await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/pay",
            new { payments = new[] { new { method = "Cash", amount = 100000 } } });
        Assert.Equal(40000, paid.Dec("changeAmount"));
        var delivered = await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/deliver");

        Assert.Equal("Delivered", delivered.Str("status"));
        Assert.Equal(cashBefore + 60000, await ShiftCash(cashier));
        var shift = await cashier.Get("/api/cafe/shifts/current");
        Assert.Contains(shift["otherTakings"]!.AsArray(), t => t!.Str("source") == "Rửa xe" && t!.Dec("revenue") >= 60000);
    }

    [Fact]
    public async Task Membership_covers_a_wash_and_cancelling_the_ticket_gives_the_use_back()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var package = (await cashier.Get("/api/carwash/packages")).Arr().First(p => p!.Str("code") == "GOI-BOT-10")!;

        var membership = await cashier.Post("/api/carwash/memberships", new
        {
            packageId = package.Id(), customerPhone = phone, customerName = "Anh Tâm",
            payments = new[] { new { method = "Transfer", amount = 1000000 } },
        });
        Assert.Equal(12, membership["remainingUses"]!.GetValue<int>());

        var ticket = await Intake(cashier, "RX-BOT", membership.Id(), phone);
        Assert.Equal(0, ticket.Dec("total"));
        Assert.Equal(100000, ticket.Dec("membershipCovered"));
        var customer = await cashier.Get($"/api/carwash/customers/{ticket.Id("customerId")}");
        Assert.Equal(11, customer["memberships"]![0]!["remainingUses"]!.GetValue<int>());

        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/cancel", new { reason = "Khách không rửa nữa" });
        customer = await cashier.Get($"/api/carwash/customers/{ticket.Id("customerId")}");
        Assert.Equal(12, customer["memberships"]![0]!["remainingUses"]!.GetValue<int>());
    }

    [Fact]
    public async Task Washers_can_still_be_assigned_after_a_membership_ticket_is_settled()
    {
        var cashier = await api.LoginAs("thungan1");
        await Seeded.EnsureShiftOpen(cashier);
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var membership = await BuyMembership(cashier, phone);

        var ticket = await Intake(cashier, "RX-BOT", membership.Id(), phone, withStaff: false);
        ticket = await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/pay", new { payments = Array.Empty<object>() }); // 0 VND
        Assert.True(ticket["isPaid"]!.GetValue<bool>());
        Assert.False(ticket["isEditable"]!.GetValue<bool>());

        var washers = await WasherIds(cashier);
        ticket = await cashier.Put($"/api/carwash/tickets/{ticket.Id()}/staff", new { staffIds = washers });
        Assert.Equal(washers.Length, ticket["staff"]!.AsArray().Count);
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        ticket = await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/complete");

        Assert.False(ticket["canAssignStaff"]!.GetValue<bool>());
        var locked = await Assert.ThrowsAsync<HttpRequestException>(() =>
            cashier.Put($"/api/carwash/tickets/{ticket.Id()}/staff", new { staffIds = washers.Take(1) }));
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
    }

    [Fact]
    public async Task Plate_lookup_finds_the_car_its_owner_and_the_ticket_in_progress()
    {
        var cashier = await api.LoginAs("thungan1");
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var ticket = await Intake(cashier, "HUT-BUI", phone: phone);

        var lookup = await cashier.Get($"/api/carwash/vehicles/lookup?plate={Uri.EscapeDataString(ticket.Str("plate").ToLower())}");
        Assert.Equal(phone, lookup["customer"]!.Str("phone"));
        Assert.Equal(ticket.Str("code"), lookup["openTicket"]!.Str("code"));

        var quickWash = await ServiceId(cashier, "RX-THUONG");
        var again = await Assert.ThrowsAsync<HttpRequestException>(() => cashier.Post("/api/carwash/tickets", new
        {
            plate = ticket.Str("plate"), vehicleType = "Sedan",
            services = new[] { new { offeringId = quickWash, membershipId = (Guid?)null } },
        }));
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Washer_works_the_queue_but_cannot_take_payment()
    {
        var washer = await api.LoginAs("ruaxe1");
        var ticket = await Intake(washer, "RX-THUONG");
        await washer.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        Assert.Contains((await washer.Get("/api/carwash/tickets/queue")).Arr(), t => t!.Id() == ticket.Id());

        var pay = await Assert.ThrowsAsync<HttpRequestException>(() => washer.Post($"/api/carwash/tickets/{ticket.Id()}/pay",
            new { payments = new[] { new { method = "Cash", amount = 60000 } } }));
        Assert.Equal(HttpStatusCode.Forbidden, pay.StatusCode);
    }

    [Fact]
    public async Task Commission_report_sums_completed_washes_per_washer()
    {
        var cashier = await api.LoginAs("thungan1");
        var manager = await api.LoginAs("quanly1");
        var ticket = await Intake(cashier, "DANH-BONG"); // fixed 50,000 commission, split between 2 washers
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/start");
        await cashier.Post($"/api/carwash/tickets/{ticket.Id()}/complete");

        var today = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
        var report = await manager.Get($"/api/reports/carwash-commissions?from={today}&to={today}");
        var washer = report["staff"]!.AsArray().First(s => s!.Str("fullName") == "Ngô Văn Rửa")!;
        Assert.True(washer.Dec("commission") >= 25000);
    }

    [Fact]
    public async Task Condition_photos_accept_images_only_and_stay_in_their_branch()
    {
        var cashier = await api.LoginAs("thungan1");
        var otherBranch = await api.LoginAs("quanly2");
        var ticket = await Intake(cashier, "RX-THUONG");
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6];

        var uploaded = await Upload(cashier, ticket.Id(), png, "truoc.png");
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var photoId = JsonNode.Parse(await uploaded.Content.ReadAsStringAsync())!.Id();
        Assert.Equal(png, await cashier.GetByteArrayAsync($"/api/carwash/tickets/{ticket.Id()}/photos/{photoId}"));

        var fake = await Upload(cashier, ticket.Id(), "<script>alert(1)</script>"u8.ToArray(), "xe.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, fake.StatusCode);

        var leak = await otherBranch.GetAsync($"/api/carwash/tickets/{ticket.Id()}/photos/{photoId}");
        Assert.Equal(HttpStatusCode.NotFound, leak.StatusCode);
    }

    [Fact]
    public async Task Tickets_of_another_branch_are_invisible()
    {
        var cashier = await api.LoginAs("thungan1");
        var otherBranch = await api.LoginAs("quanly2");
        var ticket = await Intake(cashier, "RX-THUONG");

        Assert.DoesNotContain((await otherBranch.Get("/api/carwash/tickets/queue")).Arr(), t => t!.Id() == ticket.Id());
        var get = await otherBranch.GetAsync($"/api/carwash/tickets/{ticket.Id()}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        var start = await otherBranch.PostAsync($"/api/carwash/tickets/{ticket.Id()}/start", null);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);
    }

    [Fact]
    public async Task Washer_cannot_change_a_customers_phone()
    {
        var cashier = await api.LoginAs("thungan1");
        var washer = await api.LoginAs("ruaxe1");
        var ticket = await Intake(cashier, "RX-THUONG", phone: $"09{Random.Shared.Next(10000000, 99999999)}");

        var edit = await washer.PutAsJsonAsync($"/api/carwash/customers/{ticket.Id("customerId")}",
            new { name = "Người khác", phone = "0999999999", note = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
    }

    [Fact]
    public async Task History_and_customer_search_find_a_visit_by_plate_or_phone()
    {
        var cashier = await api.LoginAs("thungan1");
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var ticket = await Intake(cashier, "RX-THUONG", phone: phone);
        var today = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd");
        var typedPlate = Uri.EscapeDataString(ticket.Str("plate")[3..].ToLower()); // part of the plate, typed loosely

        var history = await cashier.Get($"/api/carwash/tickets?from={today}&to={today}&q={typedPlate}");
        Assert.Contains(history.Arr(), t => t!.Id() == ticket.Id());

        var customers = await cashier.Get($"/api/carwash/customers?q={phone[^6..]}");
        var customer = customers.Arr().Single(c => c!.Str("phone") == phone)!;
        Assert.Contains(customer["plates"]!.AsArray(), p => p!.GetValue<string>() == ticket.Str("plate"));
    }

    private static Task<HttpResponseMessage> Upload(HttpClient c, Guid ticketId, byte[] bytes, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // the server must ignore what the client claims
        return c.PostAsync($"/api/carwash/tickets/{ticketId}/photos", new MultipartFormDataContent { { file, "file", fileName } });
    }
}
