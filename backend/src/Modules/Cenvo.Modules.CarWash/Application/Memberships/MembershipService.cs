using Cenvo.Modules.CarWash.Application.Customers;

namespace Cenvo.Modules.CarWash.Application.Memberships;

/// <summary>Selling a prepaid package (cash goes into the cashier's shift) and cancelling an unused one.</summary>
public class MembershipService(
    AppDbContext db,
    ICashierShifts shifts,
    IDocumentNumberGenerator numbers,
    ICurrentUser user)
{
    public async Task<MembershipDto> Sell(SellMembershipInput input, CancellationToken ct)
    {
        var shiftId = await shifts.CurrentShiftId(ct) ?? throw new BusinessException(CarWashErrors.ShiftNotOpen);
        var branchId = user.BranchId ?? throw new BusinessException(CarWashErrors.NoBranch);
        var package = await db.Set<MembershipPackage>().FindAsync([input.PackageId], ct)
                      ?? throw new NotFoundException("gói thành viên", input.PackageId);
        var customer = await db.FindOrCreateByPhone(input.CustomerPhone, input.CustomerName, ct)
                       ?? throw new BusinessException(CarWashErrors.CustomerPhoneRequired);

        var now = DateTime.UtcNow;
        var code = await numbers.Next(branchId, CustomerMembership.CodePrefix, now, ct);
        var membership = CustomerMembership.Sell(code, branchId, customer.Id, package, shiftId, user.UserId, input.Payments, now);
        db.Add(membership);
        await db.SaveChangesAsync(ct);
        return await Get(membership.Id, ct);
    }

    public async Task<MembershipDto> Cancel(Guid id, CancelMembershipInput input, CancellationToken ct)
    {
        var membership = await db.Set<CustomerMembership>().FindAsync([id], ct) ?? throw new NotFoundException("gói thành viên", id);
        membership.Cancel(input.Reason, byManager: user.IsManager(), user.BranchId, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return await Get(id, ct);
    }

    private async Task<MembershipDto> Get(Guid id, CancellationToken ct)
    {
        var customerId = await db.Set<CustomerMembership>().Where(m => m.Id == id).Select(m => m.CustomerId).FirstAsync(ct);
        return await db.MembershipsOf([customerId], DateTime.UtcNow).FirstAsync(m => m.Id == id, ct);
    }
}
