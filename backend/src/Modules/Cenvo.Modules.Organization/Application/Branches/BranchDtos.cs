namespace Cenvo.Modules.Organization.Application.Branches;

public record BranchInput(string Code, string Name, string? Address, string? Phone, bool AllowNegativeStock);

public record BranchDto(Guid Id, string Code, string Name, string? Address, string? Phone, bool AllowNegativeStock);
