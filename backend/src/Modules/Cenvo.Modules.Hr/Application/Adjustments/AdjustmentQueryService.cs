namespace Cenvo.Modules.Hr.Application.Adjustments;

/// <summary>Advances, bonuses and penalties of the current branch (the whole chain for the owner viewing all branches).</summary>
public class AdjustmentQueryService(AppDbContext db, ICurrentUser user, IStaffDirectory staff, IUserAccounts accounts)
{
    /// <param name="from">Vietnam dates, inclusive.</param>
    public async Task<List<AdjustmentDto>> List(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await db.Set<PayAdjustment>().AsNoTracking()
            .Where(a => a.Date >= from && a.Date <= to)
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.CreatedAt)
            .Take(Limits.MaxListSize)
            .Select(a => new AdjustmentDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                Kind = a.Kind,
                Amount = a.Amount,
                Date = a.Date,
                Reason = a.Reason,
                RecordedByName = a.RecordedByName,
                RecordedBy = a.CreatedBy,
                Status = a.Status,
                CancelReason = a.CancelReason,
            })
            .ToListAsync(ct);

        var names = (await accounts.List(rows.Select(r => r.EmployeeId).Distinct().ToList(), ct)).ToDictionary(u => u.Id, u => u.FullName);
        var isOwner = user.IsInRole(Roles.Admin);
        return rows.Select(r => r with
        {
            EmployeeName = names.GetValueOrDefault(r.EmployeeId, ""),
            CanCancel = r.Status == AdjustmentStatus.Active && (isOwner || r.RecordedBy == user.UserId),
        }).ToList();
    }

    /// <summary>Who can be given an adjustment here: active staff of the current branch.</summary>
    public Task<List<StaffMember>> Staff(CancellationToken ct) =>
        staff.InBranch(user.BranchId ?? throw new BusinessException(HrErrors.NoBranch), role: null, ct);
}
