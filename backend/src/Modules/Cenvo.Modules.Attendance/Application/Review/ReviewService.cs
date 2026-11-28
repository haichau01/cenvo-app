namespace Cenvo.Modules.Attendance.Application.Review;

/// <summary>
/// Branch managers fix and approve punches of their branch. Nobody but the owner (Admin) reviews their own punches,
/// so a manager cannot pay themselves overtime.
/// </summary>
public class ReviewService(AppDbContext db, ICurrentUser user, IShiftRoster roster, IStaffDirectory staff)
{
    public Task Correct(Guid id, CorrectInput input, CancellationToken ct) =>
        Change(id, r => r.Correct(input.CheckInAt, input.CheckOutAt, input.Reason, Me(), DateTime.UtcNow), ct);

    public Task Approve(Guid id, CancellationToken ct) => Change(id, r => r.Approve(Me()), ct);

    public Task Reject(Guid id, RejectInput input, CancellationToken ct) => Change(id, r => r.Reject(Me(), input.Reason), ct);

    public Task ApproveOvertime(Guid id, OvertimeInput input, CancellationToken ct) =>
        Change(id, r => r.ApproveOvertime(input.Minutes, Me()), ct);

    /// <summary>A punch the person forgot entirely (phone dead, POS down).</summary>
    public async Task<Guid> AddManual(ManualInput input, CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(AttendanceErrors.NoBranch);
        EnsureNotOwn(input.EmployeeId);
        var employee = (await staff.InBranch(branchId, role: null, ct)).FirstOrDefault(s => s.UserId == input.EmployeeId)
                       ?? throw new BusinessException(AttendanceErrors.NotInBranch);
        var shift = input.ShiftAssignmentId is { } shiftId
            ? await roster.Find(shiftId, ct) ?? throw new NotFoundException("ca đã xếp", shiftId)
            : null;

        var record = AttendanceRecord.Manual(branchId, employee, shift, input.WorkDate, input.CheckInAt, input.CheckOutAt,
            input.Reason, Me(), DateTime.UtcNow);
        db.Add(record);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            throw new BusinessException(AttendanceErrors.ShiftAlreadyPunched); // unique punch per planned shift
        }
        return record.Id;
    }

    private async Task Change(Guid id, Action<AttendanceRecord> action, CancellationToken ct)
    {
        var record = await db.Set<AttendanceRecord>().FirstOrDefaultAsync(r => r.Id == id, ct)
                     ?? throw new NotFoundException("lượt chấm công", id);
        EnsureNotOwn(record.EmployeeId);
        action(record);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(AttendanceErrors.ChangedElsewhere);
        }
    }

    private void EnsureNotOwn(Guid employeeId)
    {
        if (employeeId == user.UserId && !user.IsInRole(Roles.Admin)) throw new BusinessException(AttendanceErrors.OwnRecord);
    }

    private Editor Me() => new(user.UserId ?? throw new InvalidOperationException("Signed-in user without an id."), user.FullName);
}
