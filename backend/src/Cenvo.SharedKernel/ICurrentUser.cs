namespace Cenvo.SharedKernel;

public interface ICurrentUser
{
    Guid? UserId { get; }
    /// <summary>Branch the user is working in (branch_id claim).</summary>
    Guid? BranchId { get; }
    /// <summary>Chain admin without a selected branch: sees every branch.</summary>
    bool IsAllBranches { get; }
    bool IsInRole(string role);
    /// <summary>Display name, recorded on actions people answer for (discounts, approvals).</summary>
    string FullName { get; }
}
