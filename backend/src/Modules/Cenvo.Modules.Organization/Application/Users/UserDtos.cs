namespace Cenvo.Modules.Organization.Application.Users;

public record UserInput(string UserName, string FullName, string? Password, string[] Roles, Guid[] BranchIds, bool IsActive);

public record UserDto(Guid Id, string UserName, string FullName, bool IsActive, string[] Roles, Guid[] BranchIds);
