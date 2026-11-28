namespace Cenvo.Modules.Hr.Application.Adjustments;

/// <summary>Managers record advances, bonuses and penalties for staff of their branch; the owner (Admin) for anyone.</summary>
public class AdjustmentService(AppDbContext db, ICurrentUser user, IStaffDirectory staff)
{
    public async Task<Guid> Record(AdjustmentInput input, CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(HrErrors.NoBranch);
        var branchStaff = await staff.InBranch(branchId, role: null, ct);
        if (branchStaff.All(s => s.UserId != input.EmployeeId)) throw new BusinessException(HrErrors.StaffNotInBranch);

        var adjustment = PayAdjustment.Record(input.EmployeeId, input.Kind, input.Amount, input.Date, input.Reason,
            user.FullName, VnTime.Today);
        db.Add(adjustment);
        await db.SaveChangesAsync(ct);
        return adjustment.Id;
    }

    public async Task Cancel(Guid id, CancelAdjustmentInput input, CancellationToken ct)
    {
        var adjustment = await db.Set<PayAdjustment>().FirstOrDefaultAsync(a => a.Id == id, ct)
                         ?? throw new NotFoundException("khoản thưởng/phạt", id);
        adjustment.Cancel(input.Reason, user.UserId, user.IsInRole(Roles.Admin), DateTime.UtcNow);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(HrErrors.Concurrency);
        }
    }
}
