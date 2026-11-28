namespace Cenvo.Modules.Organization.Application.Pins;

public record SetPinInput(string CurrentPassword, string Pin);

/// <summary>Staff set their own punch PIN; the shop POS checks it when a phone cannot scan the QR code.</summary>
public class PinService(AppDbContext db) : IStaffPin
{
    /// <summary>Asks for the account password so a phone left signed in cannot be used to change the PIN.</summary>
    public async Task SetOwn(Guid userId, SetPinInput input, CancellationToken ct)
    {
        var user = await db.Set<AppUser>().FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException("người dùng", userId);
        if (!user.VerifyPassword(input.CurrentPassword)) throw new BusinessException(OrganizationErrors.WrongPassword);
        user.SetPin(input.Pin);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasPin(Guid userId, CancellationToken ct) =>
        await db.Set<AppUser>().AsNoTracking().AnyAsync(u => u.Id == userId && u.PinHash != null, ct);

    public async Task<StaffMember> Verify(Guid userId, string pin, Guid branchId, CancellationToken ct)
    {
        var user = await db.Set<AppUser>().Include(u => u.Branches)
                       .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.Branches.Any(b => b.BranchId == branchId), ct)
                   ?? throw new BusinessException(OrganizationErrors.NoBranchAccess);
        if (user.VerifyPin(pin, DateTime.UtcNow)) return new StaffMember(user.Id, user.FullName);

        await db.SaveChangesAsync(ct); // keep the failed attempt even though the request fails
        throw new BusinessException(OrganizationErrors.WrongPin);
    }
}
