using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace Cenvo.Modules.Organization.Application.Auth;

public class AuthService(AppDbContext db)
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(14);

    public async Task<UserIdentity> Login(string userName, string password, Guid? branchId, CancellationToken ct)
    {
        var user = await FindUser(u => u.UserName == userName, ct);
        if (user is not { IsActive: true } || !user.VerifyPassword(password))
            throw new BusinessException(OrganizationErrors.WrongCredentials);
        return await ToIdentity(user, branchId, ct);
    }

    public async Task<UserIdentity> SwitchBranch(Guid userId, Guid branchId, CancellationToken ct)
    {
        var user = await FindUser(u => u.Id == userId, ct) ?? throw new NotFoundException("người dùng", userId);
        return await ToIdentity(user, branchId, ct);
    }

    public async Task<string> IssueRefreshToken(Guid userId, Guid? branchId, CancellationToken ct)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.Add(new RefreshToken
        {
            UserId = userId, BranchId = branchId, TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.Add(RefreshLifetime),
        });
        await db.SaveChangesAsync(ct);
        return raw;
    }

    /// <summary>Rotation: revokes the old token and returns the identity so the Api can issue a new token pair.</summary>
    public async Task<UserIdentity> Refresh(string rawToken, CancellationToken ct)
    {
        var token = await FindToken(rawToken, ct);
        if (token == null || !token.IsUsable(DateTime.UtcNow)) throw new BusinessException(OrganizationErrors.SessionExpired);
        token.Revoke(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        var user = await FindUser(u => u.Id == token.UserId, ct);
        if (user is not { IsActive: true }) throw new BusinessException(OrganizationErrors.UserLocked);
        return await ToIdentity(user, token.BranchId, ct);
    }

    public async Task Revoke(string rawToken, CancellationToken ct)
    {
        var token = await FindToken(rawToken, ct);
        if (token == null) return;
        token.Revoke(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    private Task<RefreshToken?> FindToken(string rawToken, CancellationToken ct)
    {
        var hash = Hash(rawToken);
        return db.Set<RefreshToken>().FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
    }

    private Task<AppUser?> FindUser(Expression<Func<AppUser, bool>> predicate, CancellationToken ct) =>
        db.Set<AppUser>().Include(u => u.Roles).Include(u => u.Branches).ThenInclude(b => b.Branch)
            .FirstOrDefaultAsync(predicate, ct);

    private async Task<UserIdentity> ToIdentity(AppUser user, Guid? requestedBranchId, CancellationToken ct)
    {
        var branches = await AccessibleBranches(user, ct);
        if (requestedBranchId != null && branches.All(b => b.Id != requestedBranchId))
            throw new BusinessException(OrganizationErrors.NoBranchAccess);

        // Only one branch → select it automatically.
        var branchId = requestedBranchId ?? (branches.Count == 1 ? branches[0].Id : null);
        return new UserIdentity(user.Id, user.UserName, user.FullName, user.RoleNames, branchId, branches);
    }

    private async Task<List<BranchSummary>> AccessibleBranches(AppUser user, CancellationToken ct)
    {
        if (user.RoleNames.Contains(Roles.Admin))
            return await db.Set<Branch>().OrderBy(b => b.Code).Select(b => new BranchSummary(b.Id, b.Code, b.Name)).ToListAsync(ct);

        return user.Branches.Where(b => b.Branch is { IsDeleted: false })
            .Select(b => new BranchSummary(b.BranchId, b.Branch!.Code, b.Branch.Name))
            .OrderBy(b => b.Code).ToList();
    }

    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
