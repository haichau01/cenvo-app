using System.Security.Claims;

namespace Cenvo.Api.Auth;

public static class CenvoClaims
{
    public const string Branch = "branch_id";
    public const string FullName = "full_name";
}

/// <summary>Current user from the JWT; seeding / background jobs run as a <see cref="SystemUser"/> via <see cref="RunAs"/>.</summary>
public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ICurrentUser? _runAs;

    public void RunAs(ICurrentUser user) => _runAs = user;

    private ClaimsPrincipal? Principal => http.HttpContext?.User;

    public Guid? UserId => _runAs != null ? _runAs.UserId : ParseGuid(Principal?.FindFirstValue(ClaimTypes.NameIdentifier));

    public Guid? BranchId => _runAs != null ? _runAs.BranchId : ParseGuid(Principal?.FindFirstValue(CenvoClaims.Branch));

    /// <summary>An admin without a selected branch sees the whole chain; once a branch is selected, only that branch.</summary>
    public bool IsAllBranches => _runAs?.IsAllBranches ?? (IsInRole(Roles.Admin) && BranchId == null);

    public bool IsInRole(string role) => _runAs?.IsInRole(role) ?? Principal?.IsInRole(role) ?? false;

    public string FullName => Principal?.FindFirstValue(CenvoClaims.FullName) ?? "";

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
