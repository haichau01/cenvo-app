namespace Cenvo.Modules.Organization.Contracts;

/// <summary>Checks a staff member's punch PIN typed on the shop POS. The calling endpoint must be rate-limited.</summary>
public interface IStaffPin
{
    /// <summary>
    /// The person if the PIN is right and they may work in the branch; otherwise throws a BusinessException.
    /// A wrong PIN is counted (and saved) before throwing, so guessing locks the PIN.
    /// </summary>
    Task<StaffMember> Verify(Guid userId, string pin, Guid branchId, CancellationToken ct);
}
