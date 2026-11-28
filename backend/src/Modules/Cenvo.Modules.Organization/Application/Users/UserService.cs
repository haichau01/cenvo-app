namespace Cenvo.Modules.Organization.Application.Users;

public class UserService(AppDbContext db) : IStaffDirectory
{
    public Task<List<UserDto>> List(CancellationToken ct) =>
        db.Set<AppUser>().AsNoTracking().OrderBy(u => u.UserName)
            .Select(u => new UserDto(u.Id, u.UserName, u.FullName, u.IsActive,
                u.Roles.Select(r => r.Role).ToArray(), u.Branches.Select(b => b.BranchId).ToArray()))
            .ToListAsync(ct);

    public async Task<Guid> Save(Guid? id, UserInput input, CancellationToken ct)
    {
        var isNew = id == null;
        if (isNew && string.IsNullOrWhiteSpace(input.Password)) throw new BusinessException(OrganizationErrors.PasswordRequired);

        var user = isNew
            ? db.Add(new AppUser()).Entity
            : await db.Set<AppUser>().Include(x => x.Roles).Include(x => x.Branches).FirstOrDefaultAsync(x => x.Id == id, ct)
              ?? throw new NotFoundException("người dùng", id);

        user.UserName = input.UserName.Trim().ToLowerInvariant();
        user.FullName = input.FullName.Trim();
        user.IsActive = input.IsActive;
        if (!string.IsNullOrWhiteSpace(input.Password)) user.SetPassword(input.Password);
        user.SetRoles(input.Roles);
        user.SetBranches(input.BranchIds);

        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public Task<List<StaffMember>> InBranch(Guid branchId, string role, CancellationToken ct) =>
        db.Set<AppUser>().AsNoTracking()
            .Where(u => u.IsActive
                        && u.Branches.Any(b => b.BranchId == branchId)
                        && u.Roles.Any(r => r.Role == role))
            .OrderBy(u => u.FullName)
            .Take(Limits.MaxListSize)
            .Select(u => new StaffMember(u.Id, u.FullName))
            .ToListAsync(ct);
}
