using Cenvo.Modules.Hr.Domain;
using Cenvo.Modules.Hr.Domain.Calculations;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class PersonalDataTests
{
    [Theory]
    [InlineData("0901 234 567", "0901234567")]
    [InlineData("+84 901234567", "0901234567")]
    [InlineData("  ", null)]
    public void Phone_is_normalized_to_digits(string input, string? expected) => Assert.Equal(expected, PersonalData.Phone(input));

    [Theory]
    [InlineData("12345")]
    [InlineData("07912345678A")]
    public void National_id_must_be_twelve_digits(string input) =>
        Assert.Throws<BusinessException>(() => PersonalData.NationalId(input));

    [Fact]
    public void National_id_accepts_spaces() => Assert.Equal("079123456789", PersonalData.NationalId("079 123 456 789"));
}

public class SalaryHistoryTests
{
    [Fact]
    public void The_rate_in_force_is_the_latest_started_one()
    {
        var employee = Employee.For(Guid.NewGuid());
        employee.AddSalaryRate(new DateOnly(2026, 1, 1), 6000000, 0, "Khởi điểm");
        employee.AddSalaryRate(new DateOnly(2026, 7, 1), 7000000, 300000, "Tăng lương");

        Assert.Null(SalaryHistory.EffectiveOn(employee.SalaryRates, new DateOnly(2025, 12, 31)));
        Assert.Equal(6000000, SalaryHistory.EffectiveOn(employee.SalaryRates, new DateOnly(2026, 6, 30))!.MonthlySalary);
        Assert.Equal(7000000, SalaryHistory.EffectiveOn(employee.SalaryRates, new DateOnly(2026, 7, 1))!.MonthlySalary);
    }

    [Fact]
    public void A_rate_must_start_on_the_first_have_a_reason_and_a_unique_month()
    {
        var employee = Employee.For(Guid.NewGuid());
        Assert.Throws<BusinessException>(() => employee.AddSalaryRate(new DateOnly(2026, 7, 15), 7000000, 0, "Tăng"));
        Assert.Throws<BusinessException>(() => employee.AddSalaryRate(new DateOnly(2026, 7, 1), 7000000, 0, " "));
        employee.AddSalaryRate(new DateOnly(2026, 7, 1), 7000000, 0, "Tăng");
        Assert.Throws<BusinessException>(() => employee.AddSalaryRate(new DateOnly(2026, 7, 1), 8000000, 0, "Sửa"));
    }

    [Fact]
    public void Only_a_rate_that_has_not_started_can_be_removed()
    {
        var employee = Employee.For(Guid.NewGuid());
        var started = employee.AddSalaryRate(new DateOnly(2026, 11, 1), 7000000, 0, "Khởi điểm");
        var future = employee.AddSalaryRate(new DateOnly(2026, 12, 1), 7500000, 0, "Tăng");
        var today = new DateOnly(2026, 11, 28);

        Assert.Throws<BusinessException>(() => employee.RemoveSalaryRate(started.Id, today));
        employee.RemoveSalaryRate(future.Id, today);
        Assert.Single(employee.SalaryRates);
    }

    [Fact]
    public void Contract_warning_starts_thirty_days_before_the_end()
    {
        var today = new DateOnly(2026, 11, 28);
        Assert.True(ContractAlerts.EndsSoon(today.AddDays(30), today));
        Assert.False(ContractAlerts.EndsSoon(today.AddDays(31), today));
        Assert.False(ContractAlerts.EndsSoon(null, today));
    }
}

public class EmploymentTests
{
    [Fact]
    public void Leave_date_cannot_precede_hire_and_cannot_leave_twice()
    {
        var employee = Employee.For(Guid.NewGuid());
        employee.Update(Department.Cafe, "Thu ngân", new DateOnly(2026, 3, 1), null, null, null, null, null);
        Assert.Throws<BusinessException>(() => employee.Leave(new DateOnly(2026, 2, 28)));
        employee.Leave(new DateOnly(2026, 11, 20));
        Assert.Throws<BusinessException>(() => employee.Leave(new DateOnly(2026, 11, 21)));
        employee.Rehire();
        Assert.False(employee.HasLeft);
    }

    [Fact]
    public void Contract_end_must_follow_its_start()
    {
        var employee = Employee.For(Guid.NewGuid());
        Assert.Throws<BusinessException>(() => employee.AddContract(ContractType.FullTime, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), null));
        Assert.Null(employee.AddContract(ContractType.FullTime, new DateOnly(2026, 1, 1), null, null).EndDate);
    }
}

public class PayAdjustmentTests
{
    private static readonly DateOnly Today = new(2026, 11, 28);
    private static readonly Guid Manager = Guid.NewGuid();

    private static PayAdjustment Recorded()
    {
        var adjustment = PayAdjustment.Record(Guid.NewGuid(), AdjustmentKind.Penalty, 50000.4m, Today, "Đi trễ", "Quản", Today);
        adjustment.CreatedBy = Manager;
        return adjustment;
    }

    [Fact]
    public void Amount_is_rounded_to_dong_and_must_be_positive_with_a_reason_not_in_the_future()
    {
        Assert.Equal(50000, Recorded().Amount);
        Assert.Throws<BusinessException>(() => PayAdjustment.Record(Guid.NewGuid(), AdjustmentKind.Bonus, 0, Today, "Thưởng", "Q", Today));
        Assert.Throws<BusinessException>(() => PayAdjustment.Record(Guid.NewGuid(), AdjustmentKind.Bonus, 10, Today, " ", "Q", Today));
        Assert.Throws<BusinessException>(() => PayAdjustment.Record(Guid.NewGuid(), AdjustmentKind.Bonus, 10, Today.AddDays(1), "x", "Q", Today));
    }

    [Fact]
    public void Only_the_recorder_or_the_owner_may_cancel()
    {
        Assert.Throws<BusinessException>(() => Recorded().Cancel("Nhầm", Guid.NewGuid(), byOwner: false, DateTime.UtcNow));
        Recorded().Cancel("Nhầm", Guid.NewGuid(), byOwner: true, DateTime.UtcNow);

        var own = Recorded();
        own.Cancel("Nhầm", Manager, byOwner: false, DateTime.UtcNow);
        Assert.Equal(AdjustmentStatus.Cancelled, own.Status);
        Assert.Throws<BusinessException>(() => own.Cancel("Lần nữa", Manager, byOwner: false, DateTime.UtcNow));
    }
}
