using Cenvo.Modules.Attendance.Contracts;
using Cenvo.Modules.Attendance.Domain;
using Cenvo.Modules.Attendance.Domain.Calculations;
using Cenvo.Modules.Organization.Contracts;
using Cenvo.Modules.Organization.Domain;
using Cenvo.Modules.Schedule.Contracts;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class PunchCodeTests
{
    private sealed class Signer(byte key) : IPunchCodeSigner
    {
        public byte[] Sign(string payload) => System.Security.Cryptography.HMACSHA256.HashData(new[] { key }, System.Text.Encoding.UTF8.GetBytes(payload));
    }

    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly PunchOrigin Origin = new(Branch, Guid.NewGuid());
    private static readonly DateTime Now = new(2026, 12, 7, 1, 0, 10, DateTimeKind.Utc);

    [Fact]
    public void Code_is_accepted_during_its_window_and_the_next_one_only()
    {
        var code = PunchCode.Issue(Origin, Now, new Signer(1));
        Assert.Equal(Origin, PunchCode.Read(code, Now.AddSeconds(25), new Signer(1)));
        Assert.Equal(Origin, PunchCode.Read(code, Now.AddSeconds(45), new Signer(1)));
        Assert.Null(PunchCode.Read(code, Now.AddSeconds(55), new Signer(1)));
        Assert.Equal(new DateTime(2026, 12, 7, 1, 1, 0, DateTimeKind.Utc), PunchCode.ExpiresAt(Now));
    }

    [Fact]
    public void Forged_or_tampered_codes_are_refused()
    {
        var code = PunchCode.Issue(Origin, Now, new Signer(1));
        Assert.Null(PunchCode.Read(code, Now, new Signer(2)));
        Assert.Null(PunchCode.Read(code.Replace(Origin.ShownBy.ToString("N"), Guid.NewGuid().ToString("N")), Now, new Signer(1)));
        Assert.Null(PunchCode.Read(code.Replace(Branch.ToString("N"), Guid.NewGuid().ToString("N")), Now, new Signer(1)));
        Assert.Null(PunchCode.Read("not-a-code", Now, new Signer(1)));
        Assert.Null(PunchCode.Read(null, Now, new Signer(1)));
    }
}

public class AttendanceRecordTests
{
    private static readonly StaffMember Thu = new(Guid.NewGuid(), "Trần Thị Thu");
    private static readonly Editor Manager = new(Guid.NewGuid(), "Quản lý");
    private static readonly DateOnly Day = new(2026, 12, 7);
    private static readonly DateTime Start = new(2026, 12, 7, 1, 0, 0, DateTimeKind.Utc); // 08:00 VN
    private static readonly DateTime End = Start.AddHours(8);

    private static RosterShift Shift() => new()
    {
        AssignmentId = Guid.NewGuid(), BranchId = Guid.NewGuid(), EmployeeId = Thu.UserId, EmployeeName = Thu.FullName, Date = Day,
        ShiftName = "Sáng", StartsAt = Start, EndsAt = End,
    };

    private static AttendanceRecord Punched(DateTime checkIn, DateTime checkOut, RosterShift? shift = null)
    {
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Thu, shift, checkIn, PunchMethod.Qr);
        record.CheckOut(checkOut, PunchMethod.Qr);
        return record;
    }

    [Fact]
    public void Late_early_and_overtime_are_measured_against_the_planned_shift()
    {
        var late = Punched(Start.AddMinutes(12).AddSeconds(50), End.AddMinutes(-5), Shift());
        Assert.Equal(12, late.LateMinutes);
        Assert.Equal(5, late.EarlyLeaveMinutes);
        Assert.Equal(0, late.ProposedOvertimeMinutes);
        Assert.True(late.Counts);

        var stayed = Punched(Start.AddMinutes(-20), End.AddMinutes(40), Shift());
        Assert.Equal(0, stayed.LateMinutes); // coming early is not overtime
        Assert.Equal(40, stayed.ProposedOvertimeMinutes);
        Assert.Equal(40, stayed.OvertimeLimitMinutes);

        var lingered = Punched(Start, End.AddMinutes(10), Shift());
        Assert.Equal(0, lingered.ProposedOvertimeMinutes); // under 15 minutes: not put up for review
        Assert.Equal(10, lingered.OvertimeLimitMinutes);
    }

    [Fact]
    public void Overtime_cannot_exceed_the_time_after_the_shift()
    {
        var record = Punched(Start, End.AddMinutes(40), Shift());
        Assert.Throws<BusinessException>(() => record.ApproveOvertime(41, Manager));
        record.ApproveOvertime(30, Manager);
        Assert.Equal(30, record.OvertimeApprovedMinutes);
    }

    [Fact]
    public void Unplanned_work_counts_only_after_approval()
    {
        var record = Punched(Start, End);
        Assert.Equal(ApprovalStatus.Pending, record.Approval);
        Assert.False(record.Counts);
        Assert.Throws<BusinessException>(() => record.ApproveOvertime(10, Manager));

        record.Approve(Manager);
        Assert.True(record.Counts);
        Assert.Equal(Day, record.WorkDate); // 08:00 VN
        Assert.Throws<BusinessException>(() => record.Reject(Manager, "Nhầm"));
    }

    [Fact]
    public void Double_tap_is_not_a_check_out()
    {
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Thu, Shift(), Start, PunchMethod.Qr);
        Assert.Throws<BusinessException>(() => record.CheckOut(Start.AddSeconds(30), PunchMethod.Qr));
    }

    [Fact]
    public void Forgotten_check_out_is_flagged_four_hours_after_the_shift()
    {
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Thu, Shift(), Start, PunchMethod.Pin);
        Assert.False(record.IsForgotten(End.AddHours(3)));
        Assert.True(record.IsForgotten(End.AddHours(5)));
    }

    [Fact]
    public void Correction_keeps_history_needs_a_reason_and_resets_too_much_overtime()
    {
        var record = Punched(Start, End.AddMinutes(60), Shift());
        record.ApproveOvertime(60, Manager);
        Assert.Throws<BusinessException>(() => record.Correct(Start, End, " ", Manager, End.AddDays(1)));
        Assert.Throws<BusinessException>(() => record.Correct(End, Start, "Sai giờ", Manager, End.AddDays(1)));
        Assert.Throws<BusinessException>(() => record.Correct(Start, End.AddDays(2), "Sai giờ", Manager, End.AddDays(1)));

        record.Correct(Start, End.AddMinutes(20), "Quên chấm ra, về lúc 16:20", Manager, End.AddDays(1));
        Assert.Null(record.OvertimeApprovedMinutes);
        var edit = Assert.Single(record.Edits);
        Assert.Equal(End.AddMinutes(60), edit.OldCheckOut);
        Assert.Equal(End.AddMinutes(20), edit.NewCheckOut);
    }

    [Fact]
    public void Overnight_shift_counts_for_the_day_it_starts()
    {
        var night = Shift() with { StartsAt = new DateTime(2026, 12, 7, 15, 0, 0, DateTimeKind.Utc), EndsAt = new DateTime(2026, 12, 7, 23, 0, 0, DateTimeKind.Utc) };
        var record = AttendanceRecord.CheckIn(Guid.NewGuid(), Thu, night, night.StartsAt.AddMinutes(5), PunchMethod.Qr);
        Assert.Equal(Day, record.WorkDate);
    }
}

public class TimesheetTests
{
    private static readonly DateOnly Holiday = new(2026, 12, 25);

    private static PunchFact Worked(DateOnly day, Guid? shift = null, int late = 0, int overtime = 0, int? approved = null) =>
        new(day, shift ?? Guid.NewGuid(), Counts: true, Pending: false, Forgotten: false, WorkedMinutes: 480, LateMinutes: late,
            EarlyLeaveMinutes: 0, ProposedOvertimeMinutes: overtime, OvertimeApprovedMinutes: approved);

    [Fact]
    public void Paid_days_count_holidays_worked_by_multiplier_and_holidays_off_as_one()
    {
        var holidays = new Dictionary<DateOnly, decimal> { [Holiday] = 3m, [new DateOnly(2026, 12, 31)] = 2m };
        var punches = new[]
        {
            Worked(new DateOnly(2026, 12, 1), late: 5),
            Worked(new DateOnly(2026, 12, 1)), // second shift the same day: still one day
            Worked(new DateOnly(2026, 12, 2), overtime: 60, approved: 45),
            Worked(Holiday, overtime: 30, approved: 30),
        };

        var totals = Timesheet.Calculate(punches, [], holidays);
        Assert.Equal(2, totals.WorkDays);
        Assert.Equal(1, totals.HolidaysWorked);
        Assert.Equal(1, totals.HolidaysOff);
        Assert.Equal(2 + 3 + 1, totals.PaidDays);
        Assert.Equal(45, totals.OvertimeMinutes);
        Assert.Equal(30, totals.HolidayOvertimeMinutes);
        Assert.Equal(1, totals.LateCount);
        Assert.Equal(4 * 480, totals.WorkedMinutes);
    }

    [Fact]
    public void Pending_and_unpunched_shifts_do_not_count()
    {
        var punchedShift = Guid.NewGuid();
        var punches = new[]
        {
            Worked(new DateOnly(2026, 12, 3), punchedShift, overtime: 20),
            new PunchFact(new DateOnly(2026, 12, 4), null, Counts: false, Pending: true, Forgotten: false, 300, 0, 0, 0, null),
            new PunchFact(new DateOnly(2026, 12, 5), Guid.NewGuid(), Counts: false, Pending: false, Forgotten: true, 0, 0, 0, 0, null),
        };
        PlannedFact[] planned = [new(punchedShift, IsOver: true), new(Guid.NewGuid(), IsOver: true), new(Guid.NewGuid(), IsOver: false)];

        var totals = Timesheet.Calculate(punches, planned, new Dictionary<DateOnly, decimal>());
        Assert.Equal(1, totals.WorkDays);
        Assert.Equal(1, totals.PaidDays);
        Assert.Equal(3, totals.PlannedShifts);
        Assert.Equal(1, totals.AbsentShifts); // the future one is not absent yet
        Assert.Equal(1, totals.PendingApprovals);
        Assert.Equal(1, totals.ForgottenCheckOuts);
        Assert.Equal(20, totals.UnreviewedOvertimeMinutes);
    }
}

public class PinTests
{
    [Fact]
    public void Five_wrong_pins_lock_for_fifteen_minutes()
    {
        var user = new AppUser();
        Assert.Throws<BusinessException>(() => user.SetPin("12a4"));
        Assert.Throws<BusinessException>(() => user.SetPin("123"));
        user.SetPin("2468");

        var now = new DateTime(2026, 12, 7, 1, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < AppUser.MaxPinAttempts; i++) Assert.False(user.VerifyPin("0000", now));
        Assert.Throws<BusinessException>(() => user.VerifyPin("2468", now.AddMinutes(14)));
        Assert.True(user.VerifyPin("2468", now.AddMinutes(16)));
    }
}
