namespace Cenvo.Modules.Attendance.Application.Review;

/// <summary>Managers: the punches of their branch for a day, and what still needs them.</summary>
public class AttendanceQueryService(AppDbContext db, IShiftRoster roster)
{
    /// <summary>How far back "to review" looks (a month and a bit: last month's payroll may still be open).</summary>
    public const int ReviewDays = 62;

    public async Task<AttendanceDayDto> Day(DateOnly date, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var records = await Rows(db.Set<AttendanceRecord>().Where(r => r.WorkDate == date).OrderBy(r => r.CheckInAt), now, ct);
        var punchedShifts = await db.Set<AttendanceRecord>().AsNoTracking()
            .Where(r => r.WorkDate == date && r.ShiftAssignmentId != null)
            .Select(r => r.ShiftAssignmentId!.Value)
            .ToListAsync(ct);

        var unpunched = (await roster.Between(date, date, employeeId: null, ct))
            .Where(s => !punchedShifts.Contains(s.AssignmentId))
            .Select(s => new UnpunchedShiftDto(s.AssignmentId, s.EmployeeId, s.EmployeeName, s.ShiftName, s.StartsAt, s.EndsAt, s.EndsAt <= now))
            .ToList();
        return new AttendanceDayDto(date, records, unpunched);
    }

    /// <summary>Unplanned punches waiting for approval, forgotten check-outs and overtime not reviewed yet.</summary>
    public async Task<List<AttendanceRowDto>> ToReview(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since = VnTime.Today.AddDays(-ReviewDays);
        var forgottenBefore = now - AttendanceRecord.ForgottenAfter;
        var minOvertime = TimeSpan.FromMinutes(AttendanceRecord.MinProposedOvertimeMinutes);
        var candidates = db.Set<AttendanceRecord>()
            .Where(r => r.WorkDate >= since)
            .Where(r => r.Approval == ApprovalStatus.Pending
                        || (r.CheckOutAt == null && r.CheckInAt < forgottenBefore)
                        || (r.OvertimeApprovedMinutes == null && r.CheckOutAt != null && r.ScheduledEnd != null
                            && r.CheckOutAt >= r.ScheduledEnd + minOvertime))
            .OrderBy(r => r.WorkDate).ThenBy(r => r.CheckInAt);
        var rows = await Rows(candidates, now, ct);
        return rows.Where(r => r.Approval == ApprovalStatus.Pending || r.Forgotten
                               || (r.ProposedOvertimeMinutes > 0 && r.OvertimeApprovedMinutes == null)).ToList();
    }

    public Task<List<AttendanceEditDto>> Edits(Guid id, CancellationToken ct) =>
        db.Set<AttendanceRecord>().AsNoTracking()
            .Where(r => r.Id == id)
            .SelectMany(r => r.Edits)
            .OrderBy(e => e.At)
            .Select(e => new AttendanceEditDto
            {
                At = e.At, EditorName = e.EditorName, OldCheckIn = e.OldCheckIn, OldCheckOut = e.OldCheckOut,
                NewCheckIn = e.NewCheckIn, NewCheckOut = e.NewCheckOut, Reason = e.Reason,
            })
            .ToListAsync(ct);

    /// <summary>Late / overtime are rules on the entity, so rows are read whole (a day or a review list is small) and mapped.</summary>
    internal static async Task<List<AttendanceRowDto>> Rows(IQueryable<AttendanceRecord> records, DateTime now, CancellationToken ct)
    {
        var rows = await records.AsNoTracking()
            .Take(Limits.MaxListSize)
            .Select(r => new { Record = r, EditCount = r.Edits.Count })
            .ToListAsync(ct);
        return rows.Select(x => AttendanceRowDto.From(x.Record, x.EditCount, now)).ToList();
    }
}
