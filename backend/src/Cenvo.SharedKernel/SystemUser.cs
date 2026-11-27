namespace Cenvo.SharedKernel;

/// <summary>System identity for seeding / background jobs: explicit Admin role, not a real person.</summary>
public sealed record SystemUser(Guid? BranchId, bool IsAllBranches) : ICurrentUser
{
    public static SystemUser AllBranches => new(null, true);
    public static SystemUser ForBranch(Guid branchId) => new(branchId, false);

    public Guid? UserId => null;
    public bool IsInRole(string role) => role == Roles.Admin;
}
