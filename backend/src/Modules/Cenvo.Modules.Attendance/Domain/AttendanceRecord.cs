using System.ComponentModel.DataAnnotations;

namespace Cenvo.Modules.Attendance.Domain;

/// <summary>
/// One check-in and (later) its check-out by one person at one branch. A punch on a planned shift copies the shift's
/// times, so late / early / overtime stay as they were even if the schedule changes later.
/// </summary>
public class AttendanceRecord : BaseEntity, IBranchScoped
{
    /// <summary>An open punch this long after the shift end (or after check-in, without a shift) is a forgotten check-out.</summary>
    public static readonly TimeSpan ForgottenAfter = TimeSpan.FromHours(4);
    public static readonly TimeSpan UnscheduledMaxLength = TimeSpan.FromHours(12);
    public static readonly TimeSpan MaxLength = TimeSpan.FromHours(24);
    /// <summary>A second scan this soon is a double tap, not a check-out.</summary>
    public static readonly TimeSpan MinGap = TimeSpan.FromMinutes(1);
    /// <summary>How early before a shift a check-in still belongs to it (early arrival is not overtime).</summary>
    public static readonly TimeSpan EarlyCheckIn = TimeSpan.FromHours(1);
    /// <summary>Staying this long after the shift end puts the overtime up for review (a few minutes is just leaving).</summary>
    public const int MinProposedOvertimeMinutes = 15;

    public Guid BranchId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    /// <summary>Day the work counts for: the shift's day (an overnight shift belongs to the day it starts), else the check-in day.</summary>
    public DateOnly WorkDate { get; set; }
    public Guid? ShiftAssignmentId { get; set; }
    public string? ShiftName { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public PunchMethod CheckInMethod { get; set; }
    public PunchMethod? CheckOutMethod { get; set; }
    /// <summary>
    /// Punched on a shop screen signed in as the person themselves: they could have opened it anywhere (e.g. their phone at
    /// home), so the manager checks these.
    /// </summary>
    public bool OnOwnScreen { get; set; }
    public ApprovalStatus Approval { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNote { get; set; }
    /// <summary>Overtime the manager agreed to pay; null = not reviewed yet.</summary>
    public int? OvertimeApprovedMinutes { get; set; }
    [Timestamp] public uint Version { get; set; }
    public List<AttendanceEdit> Edits { get; set; } = [];

    public bool IsScheduled => ShiftAssignmentId != null;
    public bool IsOpen => CheckOutAt == null;
    /// <summary>Counts as work: closed, and on a planned shift or approved.</summary>
    public bool Counts => !IsOpen && Approval is ApprovalStatus.NotNeeded or ApprovalStatus.Approved;

    public int WorkedMinutes => CheckOutAt is { } outAt ? Minutes(outAt - CheckInAt) : 0;
    public int LateMinutes => ScheduledStart is { } start && CheckInAt > start ? Minutes(CheckInAt - start) : 0;
    public int EarlyLeaveMinutes => ScheduledEnd is { } end && CheckOutAt is { } outAt && outAt < end ? Minutes(end - outAt) : 0;

    /// <summary>Time that may be paid as overtime: after the shift end; an approved extra (unplanned) shift may be paid whole.</summary>
    public int OvertimeLimitMinutes => !Counts ? 0
        : ScheduledEnd is { } end ? (CheckOutAt > end ? Minutes(CheckOutAt.Value - end) : 0)
        : WorkedMinutes;

    /// <summary>Overtime put up for the manager: at least <see cref="MinProposedOvertimeMinutes"/> after a planned shift.</summary>
    public int ProposedOvertimeMinutes => IsScheduled && OvertimeLimitMinutes >= MinProposedOvertimeMinutes ? OvertimeLimitMinutes : 0;

    public bool IsForgotten(DateTime now) =>
        IsOpen && now > (ScheduledEnd ?? CheckInAt.Add(UnscheduledMaxLength)).Add(ForgottenAfter);

    public static AttendanceRecord CheckIn(Guid branchId, StaffMember employee, RosterShift? shift, DateTime at, PunchMethod method,
        bool onOwnScreen = false)
    {
        var record = new AttendanceRecord
        {
            BranchId = branchId, EmployeeId = employee.UserId, EmployeeName = employee.FullName, CheckInAt = at, CheckInMethod = method,
            OnOwnScreen = onOwnScreen,
            WorkDate = shift?.Date ?? DateOnly.FromDateTime(VnTime.ToLocal(at)),
            Approval = shift == null ? ApprovalStatus.Pending : ApprovalStatus.NotNeeded,
        };
        record.LinkShift(shift);
        return record;
    }

    /// <summary>A manager enters a punch the person forgot entirely; without a planned shift it is approved by entering it.</summary>
    public static AttendanceRecord Manual(Guid branchId, StaffMember employee, RosterShift? shift, DateOnly workDate,
        DateTime checkIn, DateTime checkOut, string? reason, Editor editor, DateTime now)
    {
        if (shift != null && shift.EmployeeId != employee.UserId) throw new BusinessException(AttendanceErrors.ShiftOtherEmployee);
        EnsureTimes(checkIn, checkOut, now);
        var record = new AttendanceRecord
        {
            BranchId = branchId, EmployeeId = employee.UserId, EmployeeName = employee.FullName, WorkDate = shift?.Date ?? workDate,
            CheckInAt = checkIn, CheckOutAt = checkOut, CheckInMethod = PunchMethod.Manual, CheckOutMethod = PunchMethod.Manual,
            Approval = shift == null ? ApprovalStatus.Approved : ApprovalStatus.NotNeeded,
            ReviewedBy = shift == null ? editor.Name : null,
        };
        record.LinkShift(shift);
        record.Edits.Add(AttendanceEdit.Create(record, null, null, editor, Required(reason), now));
        return record;
    }

    public void CheckOut(DateTime at, PunchMethod method, bool onOwnScreen = false)
    {
        if (!IsOpen) throw new BusinessException(AttendanceErrors.AlreadyCheckedOut);
        if (at - CheckInAt < MinGap) throw new BusinessException(AttendanceErrors.JustPunched);
        CheckOutAt = at;
        CheckOutMethod = method;
        OnOwnScreen |= onOwnScreen;
    }

    /// <summary>Fixes the times (forgotten check-out, wrong scan); the old times are kept in <see cref="Edits"/>.</summary>
    public void Correct(DateTime checkIn, DateTime checkOut, string? reason, Editor editor, DateTime now)
    {
        var why = Required(reason);
        EnsureTimes(checkIn, checkOut, now);
        Edits.Add(AttendanceEdit.Create(this, checkIn, checkOut, editor, why, now));
        CheckInAt = checkIn;
        CheckOutAt = checkOut;
        CheckOutMethod ??= PunchMethod.Manual;
        if (OvertimeApprovedMinutes > OvertimeLimitMinutes) OvertimeApprovedMinutes = null; // review again
    }

    public void Approve(Editor reviewer)
    {
        if (Approval != ApprovalStatus.Pending) throw new BusinessException(AttendanceErrors.NotPending);
        Approval = ApprovalStatus.Approved;
        ReviewedBy = reviewer.Name;
    }

    public void Reject(Editor reviewer, string? reason)
    {
        if (Approval != ApprovalStatus.Pending) throw new BusinessException(AttendanceErrors.NotPending);
        Approval = ApprovalStatus.Rejected;
        ReviewedBy = reviewer.Name;
        ReviewNote = Required(reason);
        OvertimeApprovedMinutes = null;
    }

    public void ApproveOvertime(int minutes, Editor reviewer)
    {
        if (!Counts) throw new BusinessException(AttendanceErrors.OvertimeNotAllowed);
        if (minutes < 0 || minutes > OvertimeLimitMinutes) throw new BusinessException(AttendanceErrors.OvertimeTooMuch(OvertimeLimitMinutes));
        OvertimeApprovedMinutes = minutes;
        ReviewedBy = reviewer.Name;
    }

    private void LinkShift(RosterShift? shift)
    {
        if (shift == null) return;
        ShiftAssignmentId = shift.AssignmentId;
        ShiftName = shift.ShiftName;
        ScheduledStart = shift.StartsAt;
        ScheduledEnd = shift.EndsAt;
    }

    private static void EnsureTimes(DateTime checkIn, DateTime checkOut, DateTime now)
    {
        if (checkOut <= checkIn) throw new BusinessException(AttendanceErrors.CheckOutBeforeIn);
        if (checkOut - checkIn > MaxLength) throw new BusinessException(AttendanceErrors.TooLong);
        if (checkOut > now) throw new BusinessException(AttendanceErrors.FutureTime);
    }

    private static string Required(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? throw new BusinessException(AttendanceErrors.ReasonRequired) : reason.Trim();

    /// <summary>Whole minutes, rounded down (a punch at 08:00:59 is 0 minutes late).</summary>
    private static int Minutes(TimeSpan span) => (int)Math.Floor(span.TotalMinutes);
}

/// <summary>Who changed a punch — kept on the record so staff can see why their hours changed.</summary>
public record Editor(Guid UserId, string Name);
