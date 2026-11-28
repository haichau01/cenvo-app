using Cenvo.Modules.Hr.Domain;
using Cenvo.Modules.Schedule.Domain;
using Cenvo.Modules.Schedule.Domain.Calculations;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class ShiftWindowTests
{
    private static readonly DateOnly Day = new(2026, 12, 7);

    [Fact]
    public void Day_shift_is_converted_from_vietnam_time()
    {
        var window = ShiftWindow.On(Day, new TimeOnly(6, 0), new TimeOnly(12, 0));
        Assert.Equal(new DateTime(2026, 12, 6, 23, 0, 0), window.StartUtc); // 06:00 +07:00
        Assert.Equal(6m, window.Hours);
    }

    [Fact]
    public void Overnight_shift_ends_the_next_day()
    {
        var window = ShiftWindow.On(Day, new TimeOnly(22, 0), new TimeOnly(6, 0));
        Assert.Equal(8m, window.Hours);
        Assert.Equal(TimeSpan.FromHours(8), ShiftWindow.Length(new TimeOnly(22, 0), new TimeOnly(6, 0)));
    }

    [Fact]
    public void Back_to_back_shifts_do_not_overlap_but_crossing_ones_do()
    {
        var morning = ShiftWindow.On(Day, new TimeOnly(6, 0), new TimeOnly(12, 0));
        var afternoon = ShiftWindow.On(Day, new TimeOnly(12, 0), new TimeOnly(18, 0));
        var lunch = ShiftWindow.On(Day, new TimeOnly(11, 0), new TimeOnly(14, 0));
        var night = ShiftWindow.On(Day.AddDays(-1), new TimeOnly(22, 0), new TimeOnly(6, 30));
        Assert.False(morning.Overlaps(afternoon));
        Assert.True(morning.Overlaps(lunch));
        Assert.True(night.Overlaps(morning)); // yesterday's night shift runs into this morning
    }
}

public class WeekAndStaffingTests
{
    [Theory]
    [InlineData("2026-12-07", "2026-12-07")] // Monday
    [InlineData("2026-12-13", "2026-12-07")] // Sunday belongs to the week before
    [InlineData("2026-12-10", "2026-12-07")]
    public void Week_starts_on_monday(string day, string monday) =>
        Assert.Equal(DateOnly.Parse(monday), Week.StartOf(DateOnly.Parse(day)));

    [Fact]
    public void Shortfalls_count_staff_per_department_and_missing_department_as_general()
    {
        var needs = new[] { new StaffingNeed(Department.Cafe, 2), new StaffingNeed(Department.CarWash, 1), new StaffingNeed(Department.General, 1) };
        var gaps = Staffing.Shortfalls(needs, [Department.Cafe, null]);
        Assert.Equal([new Shortfall(Department.Cafe, 2, 1), new Shortfall(Department.CarWash, 1, 0)], gaps);
    }
}

public class ShiftTemplateTests
{
    private static void Update(ShiftTemplate t, string start, string end, int min = 1) =>
        t.Update("Ca", TimeOnly.Parse(start), TimeOnly.Parse(end), 1, true, [new StaffingNeed(Department.Cafe, min)]);

    [Fact]
    public void Zero_length_too_long_or_bad_headcount_is_rejected()
    {
        Assert.Throws<BusinessException>(() => Update(new ShiftTemplate(), "08:00", "08:00"));
        Assert.Throws<BusinessException>(() => Update(new ShiftTemplate(), "06:00", "23:00")); // 17 h
        Assert.Throws<BusinessException>(() => Update(new ShiftTemplate(), "06:00", "12:00", min: 51));
    }

    [Fact]
    public void Needs_are_updated_in_place_and_zero_removes_them()
    {
        var template = new ShiftTemplate();
        Update(template, "06:00", "12:00", min: 2);
        var row = template.Requirements.Single();
        Update(template, "06:00", "12:00", min: 3);
        Assert.Same(row, template.Requirements.Single());
        Assert.Equal(3, row.MinStaff);
        Update(template, "06:00", "12:00", min: 0);
        Assert.Empty(template.Requirements);
    }

    [Fact]
    public void Past_days_cannot_be_planned()
    {
        var template = new ShiftTemplate();
        Update(template, "06:00", "12:00");
        var today = new DateOnly(2026, 11, 28);
        Assert.Throws<BusinessException>(() =>
            ShiftAssignment.Create(template, new Modules.Organization.Contracts.StaffMember(Guid.NewGuid(), "A"), today.AddDays(-1), today));
        Assert.Equal(360, ShiftAssignment.Create(template, new Modules.Organization.Contracts.StaffMember(Guid.NewGuid(), "A"), today, today).Minutes);
    }
}
