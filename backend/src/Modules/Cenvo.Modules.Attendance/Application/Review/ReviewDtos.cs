using Cenvo.Modules.Attendance.Application.Holidays;

namespace Cenvo.Modules.Attendance.Application.Review;

public record CorrectInput(DateTime CheckInAt, DateTime CheckOutAt, string Reason);
public record ManualInput(Guid EmployeeId, DateOnly WorkDate, Guid? ShiftAssignmentId, DateTime CheckInAt, DateTime CheckOutAt, string Reason);
public record RejectInput(string Reason);
public record OvertimeInput(int Minutes);

public record AttendanceRowDto
{
    public required Guid Id { get; init; }
    public required Guid BranchId { get; init; }
    public string BranchName { get; init; } = "";
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required DateOnly WorkDate { get; init; }
    public required string? ShiftName { get; init; }
    public required DateTime? ScheduledStart { get; init; }
    public required DateTime? ScheduledEnd { get; init; }
    public required DateTime CheckInAt { get; init; }
    public required DateTime? CheckOutAt { get; init; }
    public required PunchMethod CheckInMethod { get; init; }
    public required PunchMethod? CheckOutMethod { get; init; }
    public required bool OnOwnScreen { get; init; }
    public required ApprovalStatus Approval { get; init; }
    public required string? ReviewedBy { get; init; }
    public required string? ReviewNote { get; init; }
    public required bool Counts { get; init; }
    public required bool Forgotten { get; init; }
    public required int WorkedMinutes { get; init; }
    public required int LateMinutes { get; init; }
    public required int EarlyLeaveMinutes { get; init; }
    public required int ProposedOvertimeMinutes { get; init; }
    /// <summary>The most overtime the manager may approve.</summary>
    public required int OvertimeLimitMinutes { get; init; }
    public required int? OvertimeApprovedMinutes { get; init; }
    public required int EditCount { get; init; }

    public static AttendanceRowDto From(AttendanceRecord r, int editCount, DateTime now) => new()
    {
        Id = r.Id, BranchId = r.BranchId, EmployeeId = r.EmployeeId, EmployeeName = r.EmployeeName, WorkDate = r.WorkDate,
        ShiftName = r.ShiftName, ScheduledStart = r.ScheduledStart, ScheduledEnd = r.ScheduledEnd,
        CheckInAt = r.CheckInAt, CheckOutAt = r.CheckOutAt, CheckInMethod = r.CheckInMethod, CheckOutMethod = r.CheckOutMethod,
        OnOwnScreen = r.OnOwnScreen, Approval = r.Approval, ReviewedBy = r.ReviewedBy, ReviewNote = r.ReviewNote, Counts = r.Counts,
        Forgotten = r.IsForgotten(now),
        WorkedMinutes = r.WorkedMinutes, LateMinutes = r.LateMinutes, EarlyLeaveMinutes = r.EarlyLeaveMinutes,
        ProposedOvertimeMinutes = r.ProposedOvertimeMinutes, OvertimeLimitMinutes = r.OvertimeLimitMinutes,
        OvertimeApprovedMinutes = r.OvertimeApprovedMinutes, EditCount = editCount,
    };
}

/// <summary>A planned shift nobody has punched for (yet): absent once <see cref="IsOver"/>.</summary>
public record UnpunchedShiftDto(Guid AssignmentId, Guid EmployeeId, string EmployeeName, string ShiftName, DateTime StartsAt,
    DateTime EndsAt, bool IsOver);

public record AttendanceDayDto(DateOnly Date, List<AttendanceRowDto> Records, List<UnpunchedShiftDto> Unpunched);

public record AttendanceEditDto
{
    public required DateTime At { get; init; }
    public required string EditorName { get; init; }
    public required DateTime? OldCheckIn { get; init; }
    public required DateTime? OldCheckOut { get; init; }
    public required DateTime NewCheckIn { get; init; }
    public required DateTime? NewCheckOut { get; init; }
    public required string Reason { get; init; }
}

public record TimesheetRowDto(Guid EmployeeId, string EmployeeName, TimesheetTotals Totals);

public record TimesheetDto(DateOnly Month, int DaysInMonth, List<HolidayDto> Holidays, List<TimesheetRowDto> Rows);

public record MyAttendanceDto(DateOnly Month, int DaysInMonth, List<HolidayDto> Holidays, TimesheetTotals Totals,
    List<AttendanceRowDto> Records);
