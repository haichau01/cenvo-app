namespace Cenvo.Modules.Organization.Contracts;

public record StaffMember(Guid UserId, string FullName);

/// <summary>Active staff of a branch — for modules that assign work to people (car wash, schedule) without the whole UserService.</summary>
public interface IStaffDirectory
{
    /// <summary>Active users allowed to work in the branch and holding the role, sorted by name.</summary>
    Task<List<StaffMember>> InBranch(Guid branchId, string role, CancellationToken ct);
}
