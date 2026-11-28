using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class HrFlowTests(ApiFixture api)
{
    private static string NewUserName() => $"nv{Random.Shared.Next(100000, 999999)}";

    private static DateOnly FirstOfMonth(int monthsFromNow)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        return new DateOnly(today.Year, today.Month, 1).AddMonths(monthsFromNow);
    }

    private static async Task<Guid> BranchId(HttpClient c, string code) =>
        (await c.Get("/api/branches")).Arr().First(b => b!.Str("code") == code)!.Id();

    private static async Task<object> NewEmployee(HttpClient admin, string userName, string nationalId = "079123456789") => new
    {
        userName, fullName = "Ngô Thị Lan", password = "123456", roles = new[] { "Cashier" },
        branchIds = new[] { await BranchId(admin, "CN01") },
        department = "Cafe", position = "Thu ngân", hireDate = "2026-11-01",
        nationalId, phone = "0901 234 567", birthDate = "2000-05-20", address = "Q.1", note = (string?)null,
    };

    private static async Task<Guid> Hire(HttpClient admin, string userName) =>
        Guid.Parse((await admin.Post("/api/hr/employees", await NewEmployee(admin, userName))).GetValue<string>());

    [Fact]
    public async Task Owner_hires_an_employee_with_a_working_account_and_private_profile()
    {
        var admin = await api.LoginAs("admin");
        var userName = NewUserName();
        var id = await Hire(admin, userName);

        var detail = await admin.Get($"/api/hr/employees/{id}");
        Assert.Equal(userName, detail["account"]!.Str("userName"));
        Assert.Equal("079123456789", detail["profile"]!.Str("nationalId"));
        Assert.Equal("0901234567", detail["profile"]!.Str("phone"));
        await api.WithDb(async db =>
        {
            var stored = await db.Database.SqlQuery<string>($"SELECT national_id AS \"Value\" FROM employee WHERE id = {id}").SingleAsync();
            Assert.StartsWith("v1:", stored); // encrypted at rest
            Assert.DoesNotContain("079123456789", stored);
        });
        Assert.Contains((await admin.Get("/api/hr/employees")).Arr(), e => e!.Id() == id && e!["hasProfile"]!.GetValue<bool>());

        var employee = await api.LoginAs(userName); // the new account signs in with its password
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/hr/employees")).StatusCode);
    }

    [Fact]
    public async Task Managers_cannot_open_hr_and_bad_input_is_rejected()
    {
        var admin = await api.LoginAs("admin");
        var manager = await api.LoginAs("quanly1");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/hr/employees")).StatusCode);

        var taken = await admin.PostAsJsonAsync("/api/hr/employees", await NewEmployee(admin, "quanly1"));
        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        var badId = await admin.PostAsJsonAsync("/api/hr/employees", await NewEmployee(admin, NewUserName(), nationalId: "12345"));
        Assert.Equal(HttpStatusCode.BadRequest, badId.StatusCode);
    }

    [Fact]
    public async Task Salary_rates_start_on_the_first_of_a_month_and_only_future_ones_can_be_removed()
    {
        var admin = await api.LoginAs("admin");
        var id = await Hire(admin, NewUserName());
        var url = $"/api/hr/employees/{id}/salary-rates";

        var midMonth = await admin.PostAsJsonAsync(url, new { effectiveFrom = FirstOfMonth(0).AddDays(14), monthlySalary = 7000000, allowance = 0, reason = "Mức khởi điểm" });
        Assert.Equal(HttpStatusCode.BadRequest, midMonth.StatusCode);

        await admin.Post(url, new { effectiveFrom = FirstOfMonth(0), monthlySalary = 7000000, allowance = 500000, reason = "Mức khởi điểm" });
        var raise = Guid.Parse((await admin.Post(url, new { effectiveFrom = FirstOfMonth(1), monthlySalary = 7500000, allowance = 500000, reason = "Tăng lương" })).GetValue<string>());

        var detail = await admin.Get($"/api/hr/employees/{id}");
        Assert.Equal(7000000, detail["currentRate"]!.Dec("monthlySalary"));
        var current = detail["salaryRates"]!.Arr().First(r => r!.Dec("monthlySalary") == 7000000)!;
        Assert.False(current["canRemove"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"{url}/{current.Id()}")).StatusCode);
        Assert.True((await admin.DeleteAsync($"{url}/{raise}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Leaving_locks_the_account_and_rehiring_unlocks_it()
    {
        var admin = await api.LoginAs("admin");
        var userName = NewUserName();
        var id = await Hire(admin, userName);

        await admin.Post($"/api/hr/employees/{id}/leave", new { leaveDate = "2026-11-20" });
        await Assert.ThrowsAsync<HttpRequestException>(() => api.LoginAs(userName));

        await admin.Post($"/api/hr/employees/{id}/rehire");
        await api.LoginAs(userName);
    }

    [Fact]
    public async Task Managers_record_adjustments_for_their_branch_and_cancel_only_their_own()
    {
        var manager = await api.LoginAs("quanly1");
        var otherManager = await api.LoginAs("quanly2");
        var cashier = await api.LoginAs("thungan1");
        var staff = (await manager.Get("/api/hr/adjustments/staff")).Arr();
        var thu = staff.First(s => s!.Str("fullName") == "Trần Thị Thu")!.Id("userId");
        var otherBranchStaff = (await otherManager.Get("/api/hr/adjustments/staff")).Arr().First(s => s!.Str("fullName") == "Đặng Thu Ngân")!.Id("userId");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).ToString("yyyy-MM-dd");

        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/hr/adjustments/staff")).StatusCode);
        var wrongBranch = await manager.PostAsJsonAsync("/api/hr/adjustments",
            new { employeeId = otherBranchStaff, kind = "Bonus", amount = 100000, date = today, reason = "Thử" });
        Assert.Equal(HttpStatusCode.BadRequest, wrongBranch.StatusCode);

        var id = Guid.Parse((await manager.Post("/api/hr/adjustments",
            new { employeeId = thu, kind = "Advance", amount = 500000, date = today, reason = "Ứng lương giữa tháng" })).GetValue<string>());
        var listed = (await manager.Get($"/api/hr/adjustments?from={today}&to={today}")).Arr().First(a => a!.Id() == id)!;
        Assert.Equal("Trần Thị Thu", listed.Str("employeeName"));
        Assert.True(listed["canCancel"]!.GetValue<bool>());

        // Another branch cannot even see it (branch filter → 404).
        var foreign = await otherManager.PostAsJsonAsync($"/api/hr/adjustments/{id}/cancel", new { reason = "Nhầm" });
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        await manager.Post($"/api/hr/adjustments/{id}/cancel", new { reason = "Ghi nhầm số tiền" });
        var cancelled = (await manager.Get($"/api/hr/adjustments?from={today}&to={today}")).Arr().First(a => a!.Id() == id)!;
        Assert.Equal("Cancelled", cancelled.Str("status"));
    }
}
