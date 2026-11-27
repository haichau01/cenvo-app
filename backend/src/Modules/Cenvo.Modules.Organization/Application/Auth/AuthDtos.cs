namespace Cenvo.Modules.Organization.Application.Auth;

public record BranchSummary(Guid Id, string Code, string Name);

/// <summary>Authenticated identity — used by the Api to issue the JWT.</summary>
public record UserIdentity(Guid UserId, string UserName, string FullName, string[] Roles, Guid? BranchId,
    IReadOnlyList<BranchSummary> Branches);
