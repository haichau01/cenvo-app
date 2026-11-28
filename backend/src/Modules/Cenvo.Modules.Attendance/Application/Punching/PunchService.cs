namespace Cenvo.Modules.Attendance.Application.Punching;

/// <summary>
/// Check-in / check-out. The first punch opens a record (on the planned shift if there is one); the next one closes it.
/// The branch comes from where the punch happens (the QR code or the shop POS), not from the person's session.
/// </summary>
public class PunchService(AppDbContext db, ICurrentUser user, IShiftRoster roster, IStaffDirectory staff, IStaffPin pins,
    IPunchCodeSigner signer, IBranchDirectory branches)
{
    /// <summary>Open punches older than this are never closed by a scan (they are forgotten check-outs for the manager).</summary>
    private static readonly TimeSpan OpenLookBack = TimeSpan.FromHours(48);

    /// <summary>Shown by the shop POS as a QR code, refreshed before <see cref="KioskCodeDto.ExpiresAt"/>.</summary>
    public KioskCodeDto KioskCode()
    {
        var now = DateTime.UtcNow;
        var origin = new PunchOrigin(CurrentBranch(), Me());
        return new KioskCodeDto(PunchCode.Issue(origin, now, signer), PunchCode.ExpiresAt(now));
    }

    /// <summary>Names to pick from before typing a PIN on the shop POS.</summary>
    public Task<List<StaffMember>> KioskStaff(CancellationToken ct) => staff.InBranch(CurrentBranch(), role: null, ct);

    public async Task<PunchResult> ByCode(PunchByCodeInput input, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var origin = PunchCode.Read(input.Code, now, signer) ?? throw new BusinessException(AttendanceErrors.InvalidCode);
        var me = Me();
        var employee = (await staff.InBranch(origin.BranchId, role: null, ct)).FirstOrDefault(s => s.UserId == me)
                       ?? throw new BusinessException(AttendanceErrors.NotInBranch);
        return await Punch(origin.BranchId, employee, PunchMethod.Qr, onOwnScreen: origin.ShownBy == me, now, ct);
    }

    public async Task<PunchResult> ByPin(PunchByPinInput input, CancellationToken ct)
    {
        var branchId = CurrentBranch();
        var employee = await pins.Verify(input.EmployeeId, input.Pin, branchId, ct);
        return await Punch(branchId, employee, PunchMethod.Pin, onOwnScreen: employee.UserId == Me(), DateTime.UtcNow, ct);
    }

    private async Task<PunchResult> Punch(Guid branchId, StaffMember employee, PunchMethod method, bool onOwnScreen, DateTime now,
        CancellationToken ct)
    {
        var open = await OpenPunch(employee.UserId, now, ct);
        if (open != null)
        {
            if (open.BranchId != branchId)
                throw new BusinessException(AttendanceErrors.OpenElsewhere((await branches.Contact(open.BranchId, ct))?.Name ?? "chi nhánh khác"));
            open.CheckOut(now, method, onOwnScreen);
            await Save(ct);
            return PunchResult.Of(PunchAction.CheckOut, open);
        }

        await EnsureNotDoubleTap(employee.UserId, now, ct);
        var record = AttendanceRecord.CheckIn(branchId, employee, await NextShift(branchId, employee.UserId, now, ct), now, method,
            onOwnScreen);
        db.Add(record);
        await Save(ct);
        return PunchResult.Of(PunchAction.CheckIn, record);
    }

    /// <summary>
    /// The person's open punch in any branch (they may scan at a branch other than the one their session is in).
    /// IgnoreQueryFilters: single table, filtered by the person — only their own record is read or closed.
    /// </summary>
    private async Task<AttendanceRecord?> OpenPunch(Guid employeeId, DateTime now, CancellationToken ct)
    {
        var since = now - OpenLookBack;
        var open = await db.Set<AttendanceRecord>().IgnoreQueryFilters()
            .Where(r => r.EmployeeId == employeeId && r.CheckOutAt == null && r.CheckInAt > since)
            .OrderByDescending(r => r.CheckInAt)
            .ToListAsync(ct);
        return open.FirstOrDefault(r => !r.IsForgotten(now));
    }

    /// <summary>IgnoreQueryFilters: single table, filtered by the person (see <see cref="OpenPunch"/>).</summary>
    private async Task EnsureNotDoubleTap(Guid employeeId, DateTime now, CancellationToken ct)
    {
        var since = now - AttendanceRecord.MinGap;
        if (await db.Set<AttendanceRecord>().IgnoreQueryFilters().AnyAsync(r => r.EmployeeId == employeeId && r.CheckOutAt > since, ct))
            throw new BusinessException(AttendanceErrors.JustPunched);
    }

    /// <summary>
    /// The running (or about to start) planned shift that has no punch yet; null = unplanned work, waits for approval.
    /// IgnoreQueryFilters: single table, filtered by the shifts of this person at the punch branch.
    /// </summary>
    private async Task<RosterShift?> NextShift(Guid branchId, Guid employeeId, DateTime now, CancellationToken ct)
    {
        var shifts = await roster.Around(branchId, employeeId, now, AttendanceRecord.EarlyCheckIn, ct);
        if (shifts.Count == 0) return null;
        var ids = shifts.Select(s => s.AssignmentId).ToList();
        var punched = await db.Set<AttendanceRecord>().IgnoreQueryFilters()
            .Where(r => r.ShiftAssignmentId != null && ids.Contains(r.ShiftAssignmentId.Value))
            .Select(r => r.ShiftAssignmentId!.Value)
            .ToListAsync(ct);
        return shifts.FirstOrDefault(s => !punched.Contains(s.AssignmentId));
    }

    private async Task Save(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) // a second device punched the same person at the same moment
        {
            throw new BusinessException(AttendanceErrors.JustPunched);
        }
    }

    private Guid Me() => user.UserId ?? throw new InvalidOperationException("Signed-in user without an id.");

    private Guid CurrentBranch() => user.BranchId ?? throw new BusinessException(AttendanceErrors.NoBranch);
}
