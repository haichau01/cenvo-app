using System.Net;
using System.Net.Http.Json;
using Cenvo.Modules.Attendance.Domain;
using Cenvo.Modules.Organization.Contracts;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AttendanceFlowTests(ApiFixture api)
{
    private static DateTime VnNow => DateTime.UtcNow.AddHours(7);
    private static DateOnly Today => DateOnly.FromDateTime(VnNow);

    private async Task<Guid> BranchId(string name) =>
        (await (await api.LoginAs("admin")).Get("/api/branches")).Arr().First(b => b!.Str("name") == name)!.Id();

    /// <summary>A new person working at Quận 1 (so tests sharing the database never meet).</summary>
    private async Task<(Guid Id, string UserName)> NewEmployee(string fullName)
    {
        var userName = $"cc{Random.Shared.Next(100000, 999999)}";
        var id = Guid.Parse((await (await api.LoginAs("admin")).Post("/api/hr/employees", new
        {
            userName, fullName, password = "123456", roles = new[] { "Barista" }, branchIds = new[] { await BranchId("Cenvo Quận 1") },
            department = "Cafe", position = "Pha chế", hireDate = "2026-01-01",
        })).GetValue<string>());
        return (id, userName);
    }

    /// <summary>A shift running now (started up to 30 minutes ago, same day).</summary>
    private static async Task<Guid> AssignNow(HttpClient manager, Guid employeeId)
    {
        var now = TimeOnly.FromDateTime(VnNow);
        var start = now.Hour == 0 && now.Minute < 30 ? new TimeOnly(0, 0) : new TimeOnly(now.Hour, now.Minute).AddMinutes(-30);
        var template = Guid.Parse((await manager.Post("/api/schedule/templates", new
        {
            name = $"Ca {start:HHmm}", startTime = start.ToString("HH:mm"), endTime = start.AddHours(4).ToString("HH:mm"),
            sortOrder = 9, isActive = true, needs = Array.Empty<object>(),
        })).GetValue<string>());
        return (await manager.Post("/api/schedule/assignments", new { date = Today, shiftTemplateId = template, employeeId })).Id();
    }

    private static async Task<string> KioskCode(HttpClient kiosk) => (await kiosk.Get("/api/attendance/kiosk/code")).Str("code");

    [Fact]
    public async Task Phone_scan_checks_in_on_the_planned_shift_and_the_manager_fixes_the_check_out()
    {
        var manager = await api.LoginAs("quanly1");
        var (employeeId, userName) = await NewEmployee("Lê Văn Quét");
        var assignment = await AssignNow(manager, employeeId);
        var phone = await api.LoginAs(userName);
        var code = await KioskCode(await api.LoginAs("thungan1"));

        var checkIn = await phone.Post("/api/attendance/punch", new { code });
        Assert.Equal("CheckIn", checkIn.Str("action"));
        Assert.False(checkIn["needsApproval"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.BadRequest, (await phone.PostAsJsonAsync("/api/attendance/punch", new { code })).StatusCode); // double tap
        Assert.Equal(HttpStatusCode.BadRequest,
            (await phone.PostAsJsonAsync("/api/attendance/punch", new { code = code[..^4] + "0000" })).StatusCode); // forged

        var day = await manager.Get($"/api/attendance/day?date={Today:yyyy-MM-dd}");
        var record = day["records"]!.Arr().First(r => r!.Id("employeeId") == employeeId)!;
        Assert.NotNull(record["shiftName"]);
        Assert.False(record["onOwnScreen"]!.GetValue<bool>()); // scanned the cashier's screen
        Assert.DoesNotContain(day["unpunched"]!.Arr(), s => s!.Id("assignmentId") == assignment);

        var fixedOut = DateTime.UtcNow.AddMinutes(-1);
        await manager.Put($"/api/attendance/records/{record.Id()}/times",
            new { checkInAt = DateTime.UtcNow.AddMinutes(-30), checkOutAt = fixedOut, reason = "Quên chấm ra" });
        Assert.Single((await manager.Get($"/api/attendance/records/{record.Id()}/edits")).Arr());

        var mine = await phone.Get($"/api/attendance/mine?month={Today:yyyy-MM-dd}");
        Assert.Equal("Cenvo Quận 1", mine["records"]!.Arr().Single()!.Str("branchName"));
        Assert.Equal(1, mine["totals"]!.Dec("workedShifts"));

        // Another branch's manager sees nothing of it and cannot touch it.
        var other = await api.LoginAs("quanly2");
        Assert.DoesNotContain((await other.Get($"/api/attendance/day?date={Today:yyyy-MM-dd}"))["records"]!.Arr(),
            r => r!.Id("employeeId") == employeeId);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/attendance/records/{record.Id()}/overtime", new { minutes = 0 })).StatusCode);
        Assert.Empty((await other.Get($"/api/attendance/records/{record.Id()}/edits")).Arr());
    }

    [Fact]
    public async Task Pin_on_the_shop_pos_punches_unplanned_work_that_waits_for_approval()
    {
        var (employeeId, userName) = await NewEmployee("Ngô Thị Pin");
        var phone = await api.LoginAs(userName);
        Assert.False((await phone.Get("/api/auth/me/pin"))["hasPin"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await phone.PutAsJsonAsync("/api/auth/me/pin", new { currentPassword = "sai-mat-khau", pin = "2468" })).StatusCode);
        await phone.Put("/api/auth/me/pin", new { currentPassword = "123456", pin = "2468" });

        var kiosk = await api.LoginAs("thungan1");
        Assert.Contains((await kiosk.Get("/api/attendance/kiosk/staff")).Arr(), s => s!.Id("userId") == employeeId);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await kiosk.PostAsJsonAsync("/api/attendance/kiosk/pin", new { employeeId, pin = "1111" })).StatusCode);
        var punch = await kiosk.Post("/api/attendance/kiosk/pin", new { employeeId, pin = "2468" });
        Assert.True(punch["needsApproval"]!.GetValue<bool>());

        var manager = await api.LoginAs("quanly1");
        var pending = (await manager.Get("/api/attendance/to-review")).Arr().First(r => r!.Id("employeeId") == employeeId)!;
        Assert.Equal("Pending", pending.Str("approval"));
        Assert.Equal("Pin", pending.Str("checkInMethod"));
        await manager.Post($"/api/attendance/records/{pending.Id()}/approve");
        Assert.DoesNotContain((await manager.Get("/api/attendance/to-review")).Arr(), r => r!.Id() == pending.Id());

        // Staff cannot use the review screens.
        Assert.Equal(HttpStatusCode.Forbidden, (await phone.GetAsync("/api/attendance/to-review")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await phone.GetAsync("/api/attendance/kiosk/code")).StatusCode);
    }

    [Fact]
    public async Task Punching_on_a_screen_signed_in_as_oneself_is_flagged()
    {
        var userName = $"tn{Random.Shared.Next(100000, 999999)}";
        var cashierId = Guid.Parse((await (await api.LoginAs("admin")).Post("/api/hr/employees", new
        {
            userName, fullName = "Vũ Thị Tự", password = "123456", roles = new[] { "Cashier" }, branchIds = new[] { await BranchId("Cenvo Quận 1") },
            department = "Cafe", position = "Thu ngân", hireDate = "2026-01-01",
        })).GetValue<string>());
        var cashier = await api.LoginAs(userName);
        await cashier.Post("/api/attendance/punch", new { code = await KioskCode(cashier) }); // e.g. the kiosk opened on their own phone

        var others = await api.LoginAs("quanly1");
        var row = (await others.Get($"/api/attendance/day?date={Today:yyyy-MM-dd}"))["records"]!.Arr().First(r => r!.Id("employeeId") == cashierId)!;
        Assert.True(row["onOwnScreen"]!.GetValue<bool>());
    }

    [Fact]
    public async Task A_manager_cannot_approve_their_own_punch()
    {
        var manager = await api.LoginAs("quanly1");
        var day = Today.AddDays(-Random.Shared.Next(1, 20));
        var recordId = Guid.Empty;
        await api.WithDb(db =>
        {
            var me = db.Set<Cenvo.Modules.Organization.Domain.AppUser>().First(u => u.UserName == "quanly1");
            var branch = db.Set<Cenvo.Modules.Organization.Domain.Branch>().First(b => b.Name == "Cenvo Quận 1");
            var at = DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(2, 0)), DateTimeKind.Utc);
            var record = AttendanceRecord.CheckIn(branch.Id, new StaffMember(me.Id, me.FullName), null, at, PunchMethod.Qr);
            record.CheckOut(at.AddHours(6), PunchMethod.Qr);
            db.Add(record);
            recordId = record.Id;
            return Task.CompletedTask;
        });
        var own = await manager.PostAsync($"/api/attendance/records/{recordId}/approve", null);
        Assert.Equal(HttpStatusCode.BadRequest, own.StatusCode);
        await (await api.LoginAs("admin")).Post($"/api/attendance/records/{recordId}/approve"); // the owner may
    }

    [Fact]
    public async Task Holidays_are_owner_only_and_a_worked_holiday_counts_its_multiplier()
    {
        var holiday = new DateOnly(Random.Shared.Next(2050, 2099), Random.Shared.Next(1, 13), Random.Shared.Next(1, 28));
        var manager = await api.LoginAs("quanly1");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await manager.PostAsJsonAsync("/api/attendance/holidays", new { date = holiday, name = "Lễ thử", multiplier = 3 })).StatusCode);
        var admin = await api.LoginAs("admin");
        await admin.Post("/api/attendance/holidays", new { date = holiday, name = "Lễ thử", multiplier = 3 });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/attendance/holidays", new { date = holiday, name = "Trùng", multiplier = 2 })).StatusCode);

        var (employeeId, _) = await NewEmployee("Đinh Văn Lễ");
        await api.WithDb(db =>
        {
            var branch = db.Set<Cenvo.Modules.Organization.Domain.Branch>().First(b => b.Name == "Cenvo Quận 1");
            var at = DateTime.SpecifyKind(holiday.ToDateTime(new TimeOnly(2, 0)), DateTimeKind.Utc);
            var record = AttendanceRecord.Manual(branch.Id, new StaffMember(employeeId, "Đinh Văn Lễ"), null, holiday, at, at.AddHours(8),
                "Đi làm ngày lễ", new Editor(Guid.NewGuid(), "Chủ quán"), at.AddDays(1));
            db.Add(record);
            return Task.CompletedTask;
        });

        var sheet = await manager.Get($"/api/attendance/timesheet?month={holiday:yyyy-MM-dd}");
        var row = sheet["rows"]!.Arr().First(r => r!.Id("employeeId") == employeeId)!["totals"]!;
        Assert.Equal(1, row.Dec("holidaysWorked"));
        Assert.Equal(3, row.Dec("paidDays") - row.Dec("holidaysOff"));
    }
}
