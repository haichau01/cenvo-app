using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ScheduleFlowTests(ApiFixture api)
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    /// <summary>A Monday far enough ahead that tests sharing the database do not meet on the same days.</summary>
    private static DateOnly FutureMonday()
    {
        var day = Today.AddDays(Random.Shared.Next(60, 3000));
        return day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
    }

    private static async Task<Guid> Template(HttpClient manager, string name, string start, string end, object[]? needs = null) =>
        Guid.Parse((await manager.Post("/api/schedule/templates",
            new { name, startTime = start, endTime = end, sortOrder = 1, isActive = true, needs = needs ?? [] })).GetValue<string>());

    private static async Task<Guid> StaffId(HttpClient manager, string fullName) =>
        (await manager.Get("/api/hr/adjustments/staff")).Arr().First(s => s!.Str("fullName") == fullName)!.Id("userId");

    private static Task<HttpResponseMessage> TryAssign(HttpClient manager, DateOnly date, Guid templateId, Guid employeeId) =>
        manager.PostAsJsonAsync("/api/schedule/assignments", new { date, shiftTemplateId = templateId, employeeId });

    private static async Task<JsonNode> Assign(HttpClient manager, DateOnly date, Guid templateId, Guid employeeId)
    {
        var res = await TryAssign(manager, date, templateId, employeeId);
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<JsonNode>())!;
    }

    [Fact]
    public async Task Week_grid_shows_assignments_and_missing_staff_back_to_back_shifts_are_fine()
    {
        var manager = await api.LoginAs("quanly1");
        var monday = FutureMonday();
        var morning = await Template(manager, "Sáng", "06:00", "12:00", [new { department = "Cafe", minStaff = 1 }]);
        var afternoon = await Template(manager, "Chiều", "12:00", "18:00");
        var thu = await StaffId(manager, "Trần Thị Thu"); // no HR profile → counted as General, not Cafe

        await Assign(manager, monday, morning, thu);
        await Assign(manager, monday, afternoon, thu); // starts exactly when the morning ends
        Assert.Equal(HttpStatusCode.BadRequest, (await TryAssign(manager, monday, morning, thu)).StatusCode);

        var week = await manager.Get($"/api/schedule/week?date={monday.AddDays(3):yyyy-MM-dd}");
        Assert.Equal(monday.ToString("yyyy-MM-dd"), week.Str("weekStart"));
        Assert.Equal(2, week["assignments"]!.Arr().Count(a => a!.Id("employeeId") == thu));
        Assert.Contains(week["shortfalls"]!.Arr(), s =>
            s!.Id("shiftTemplateId") == morning && s!.Str("date") == monday.ToString("yyyy-MM-dd") && s!.Str("department") == "Cafe");
        Assert.Equal(12, week["staff"]!.Arr().First(s => s!.Id("userId") == thu)!.Dec("weeklyHours"));
    }

    [Fact]
    public async Task Overlapping_shifts_are_refused_across_branches_and_overnight()
    {
        var admin = await api.LoginAs("admin");
        var userName = $"ca{Random.Shared.Next(100000, 999999)}";
        var branches = (await admin.Get("/api/branches")).Arr().Select(b => b!.Id()).ToArray();
        var employee = Guid.Parse((await admin.Post("/api/hr/employees", new
        {
            userName, fullName = "Hồ Văn Hai", password = "123456", roles = new[] { "Barista" }, branchIds = branches,
            department = "Cafe", position = "Pha chế", hireDate = "2026-01-01",
        })).GetValue<string>());

        var q1 = await api.LoginAs("quanly1");
        var thuDuc = await api.LoginAs("quanly2");
        var monday = FutureMonday();
        var night = await Template(q1, "Đêm", "22:00", "06:00");
        var dawn = await Template(thuDuc, "Rạng sáng", "05:00", "09:00");
        var morning = await Template(thuDuc, "Sáng", "06:00", "12:00");

        await Assign(q1, monday, night, employee); // 22:00 Monday → 06:00 Tuesday
        var clash = await TryAssign(thuDuc, monday.AddDays(1), dawn, employee);
        Assert.Equal(HttpStatusCode.BadRequest, clash.StatusCode);
        Assert.Contains("Cenvo Quận 1", await clash.Content.ReadAsStringAsync());
        await Assign(thuDuc, monday.AddDays(1), morning, employee); // starts when the night shift ends

        var mine = await (await api.LoginAs(userName)).Get(
            $"/api/schedule/mine?from={monday:yyyy-MM-dd}&to={monday.AddDays(6):yyyy-MM-dd}");
        Assert.Equal(["Cenvo Quận 1", "Cenvo Thủ Đức"], mine.Arr().Select(s => s!.Str("branchName")).ToArray());
    }

    [Fact]
    public async Task Long_weeks_warn_past_days_are_locked_and_staff_cannot_plan()
    {
        var manager = await api.LoginAs("quanly1");
        var cashier = await api.LoginAs("thungan1");
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync($"/api/schedule/week?date={Today:yyyy-MM-dd}")).StatusCode);

        var shift = await Template(manager, "Dài", "08:00", "20:00");
        var kho = await StaffId(manager, "Phạm Thị Kho");
        Assert.Equal(HttpStatusCode.BadRequest, (await TryAssign(manager, Today.AddDays(-1), shift, kho)).StatusCode);

        var monday = FutureMonday();
        var results = new List<JsonNode>();
        for (var day = 0; day < 5; day++) results.Add(await Assign(manager, monday.AddDays(day), shift, kho)); // 5 × 12 h = 60 h
        Assert.False(results[3]["overLimit"]!.GetValue<bool>()); // 48 h
        Assert.True(results[4]["overLimit"]!.GetValue<bool>());  // 60 h > 48 h: saved, but flagged
        Assert.Equal(60, results[4].Dec("weeklyHours"));
    }
}
