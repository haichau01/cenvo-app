namespace Cenvo.Modules.Organization.Application.Users;

public class UserService(AppDbContext db) : IStaffDirectory, IUserAccounts
{
    public Task<List<UserAccount>> List(CancellationToken ct) => List(null, ct);

    public Task<List<UserAccount>> List(IReadOnlyCollection<Guid>? ids, CancellationToken ct)
    {
        var users = db.Set<AppUser>().AsNoTracking();
        if (ids != null) users = users.Where(u => ids.Contains(u.Id));
        return users
            .OrderBy(u => u.FullName)
            .Take(Limits.MaxListSize)
            .Select(u => new UserAccount
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                IsActive = u.IsActive,
                Roles = u.Roles.Select(r => r.Role).ToArray(),
                BranchIds = u.Branches.Select(b => b.BranchId).ToArray(),
            })
            .ToListAsync(ct);
    }

    public async Task<Guid> Save(Guid? id, UserAccountInput input, CancellationToken ct)
    {
        var userId = await Stage(id, input, ct);
        await db.SaveChangesAsync(ct);
        return userId;
    }

    public async Task<Guid> Stage(Guid? id, UserAccountInput input, CancellationToken ct)
    {
        var userName = AppUser.NormalizeUserName(input.UserName);
        // IgnoreQueryFilters: user names stay unique even among deleted accounts (unique index); no branch data here.
        if (await db.Set<AppUser>().IgnoreQueryFilters().AnyAsync(u => u.UserName == userName && u.Id != id, ct))
            throw new BusinessException(OrganizationErrors.UserNameTaken);

        var branchIds = input.BranchIds.Distinct().ToList();
        if (await db.Set<Branch>().CountAsync(b => branchIds.Contains(b.Id), ct) != branchIds.Count)
            throw new BusinessException(OrganizationErrors.UnknownBranch);

        var user = id == null ? db.Add(new AppUser()).Entity : await Load(id.Value, ct);
        user.UpdateAccount(userName, input.FullName, input.IsActive);
        if (!string.IsNullOrWhiteSpace(input.Password)) user.SetPassword(input.Password);
        else if (id == null) throw new BusinessException(OrganizationErrors.PasswordRequired);
        user.SetRoles(input.Roles);
        user.SetBranches(branchIds);
        return user.Id;
    }

    public async Task SetActive(Guid id, bool isActive, CancellationToken ct) => (await Load(id, ct)).IsActive = isActive;

    /// <param name="role">Null = any role.</param>
    public Task<List<StaffMember>> InBranch(Guid branchId, string? role, CancellationToken ct) =>
        db.Set<AppUser>().AsNoTracking()
            .Where(u => u.IsActive
                        && u.Branches.Any(b => b.BranchId == branchId)
                        && (role == null || u.Roles.Any(r => r.Role == role)))
            .OrderBy(u => u.FullName)
            .Take(Limits.MaxListSize)
            .Select(u => new StaffMember(u.Id, u.FullName))
            .ToListAsync(ct);

    private async Task<AppUser> Load(Guid id, CancellationToken ct) =>
        await db.Set<AppUser>().Include(x => x.Roles).Include(x => x.Branches).FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new NotFoundException("người dùng", id);
}
