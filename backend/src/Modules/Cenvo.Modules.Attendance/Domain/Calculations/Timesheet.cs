namespace Cenvo.Modules.Attendance.Domain.Calculations;

/// <summary>What one punch contributes to a timesheet (taken from <see cref="AttendanceRecord"/>).</summary>
public readonly record struct PunchFact(
    DateOnly WorkDate, Guid? ShiftAssignmentId, bool Counts, bool Pending, bool Forgotten,
    int WorkedMinutes, int LateMinutes, int EarlyLeaveMinutes, int ProposedOvertimeMinutes, int? OvertimeApprovedMinutes,
    bool OnOwnScreen = false)
{
    public static PunchFact From(AttendanceRecord r, DateTime now) => new(
        r.WorkDate, r.ShiftAssignmentId, r.Counts, r.Approval == ApprovalStatus.Pending, r.IsForgotten(now),
        r.WorkedMinutes, r.LateMinutes, r.EarlyLeaveMinutes, r.ProposedOvertimeMinutes, r.OvertimeApprovedMinutes, r.OnOwnScreen);
}

/// <summary>A planned shift; <see cref="IsOver"/> = it has ended, so no punch means absent.</summary>
public readonly record struct PlannedFact(Guid AssignmentId, bool IsOver);

/// <summary>One person's month. Paid days feed payroll: salary ÷ days in month × <see cref="PaidDays"/>.</summary>
public record TimesheetTotals
{
    public int PlannedShifts { get; init; }
    public int WorkedShifts { get; init; }
    public int AbsentShifts { get; init; }
    /// <summary>Ordinary days with counted work.</summary>
    public int WorkDays { get; init; }
    public int HolidaysWorked { get; init; }
    /// <summary>Holidays of the month not worked — paid as one day each.</summary>
    public int HolidaysOff { get; init; }
    /// <summary>WorkDays + Σ multiplier of holidays worked + HolidaysOff.</summary>
    public decimal PaidDays { get; init; }
    public int WorkedMinutes { get; init; }
    public int LateCount { get; init; }
    public int LateMinutes { get; init; }
    public int EarlyLeaveCount { get; init; }
    public int OvertimeMinutes { get; init; }
    /// <summary>Approved overtime on holidays (paid at the overtime rate × the holiday multiplier).</summary>
    public int HolidayOvertimeMinutes { get; init; }
    /// <summary>Overtime still waiting for the manager.</summary>
    public int UnreviewedOvertimeMinutes { get; init; }
    public int PendingApprovals { get; init; }
    public int ForgottenCheckOuts { get; init; }
    /// <summary>Punches made on a shop screen signed in as the person themselves (worth a look).</summary>
    public int OwnScreenPunches { get; init; }
}

public static class Timesheet
{
    /// <param name="holidays">Holidays of the period → multiplier.</param>
    public static TimesheetTotals Calculate(IReadOnlyCollection<PunchFact> punches, IReadOnlyCollection<PlannedFact> planned,
        IReadOnlyDictionary<DateOnly, decimal> holidays)
    {
        var counted = punches.Where(p => p.Counts).ToList();
        var workedDates = counted.Select(p => p.WorkDate).ToHashSet();
        var punchedShifts = punches.Where(p => p.ShiftAssignmentId != null).Select(p => p.ShiftAssignmentId!.Value).ToHashSet();
        var holidaysWorked = workedDates.Where(holidays.ContainsKey).ToList();
        var workDays = workedDates.Count - holidaysWorked.Count;
        var holidaysOff = holidays.Keys.Count(d => !workedDates.Contains(d));

        return new TimesheetTotals
        {
            PlannedShifts = planned.Count,
            WorkedShifts = counted.Count(p => p.ShiftAssignmentId != null),
            AbsentShifts = planned.Count(s => s.IsOver && !punchedShifts.Contains(s.AssignmentId)),
            WorkDays = workDays,
            HolidaysWorked = holidaysWorked.Count,
            HolidaysOff = holidaysOff,
            PaidDays = workDays + holidaysWorked.Sum(d => holidays[d]) + holidaysOff,
            WorkedMinutes = counted.Sum(p => p.WorkedMinutes),
            LateCount = counted.Count(p => p.LateMinutes > 0),
            LateMinutes = counted.Sum(p => p.LateMinutes),
            EarlyLeaveCount = counted.Count(p => p.EarlyLeaveMinutes > 0),
            OvertimeMinutes = counted.Where(p => !holidays.ContainsKey(p.WorkDate)).Sum(p => p.OvertimeApprovedMinutes ?? 0),
            HolidayOvertimeMinutes = counted.Where(p => holidays.ContainsKey(p.WorkDate)).Sum(p => p.OvertimeApprovedMinutes ?? 0),
            UnreviewedOvertimeMinutes = counted.Where(p => p.OvertimeApprovedMinutes == null).Sum(p => p.ProposedOvertimeMinutes),
            PendingApprovals = punches.Count(p => p.Pending),
            ForgottenCheckOuts = punches.Count(p => p.Forgotten),
            OwnScreenPunches = punches.Count(p => p.OnOwnScreen),
        };
    }
}
