namespace Cenvo.SharedKernel;

public interface ICurrentUser
{
    Guid? UserId { get; }
    /// <summary>Branch the user is working in (branch_id claim).</summary>
    Guid? BranchId { get; }
    /// <summary>Chain admin without a selected branch: sees every branch.</summary>
    bool IsAllBranches { get; }
    bool IsInRole(string role);
}
