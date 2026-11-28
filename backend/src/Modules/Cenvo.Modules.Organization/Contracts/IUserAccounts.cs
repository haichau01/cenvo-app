namespace Cenvo.Modules.Organization.Contracts;

public record UserAccountInput(string UserName, string FullName, string? Password, string[] Roles, Guid[] BranchIds, bool IsActive);

/// <remarks>Built in SQL → init properties.</remarks>
public record UserAccount
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public required string FullName { get; init; }
    public required bool IsActive { get; init; }
    public required string[] Roles { get; init; }
    public required Guid[] BranchIds { get; init; }
}

/// <summary>
/// Login accounts for modules that manage people (HR) without the whole UserService.
/// Writes are only staged in the change tracker: the caller saves together with its own data (one transaction).
/// </summary>
public interface IUserAccounts
{
    /// <summary>Creates (<paramref name="id"/> null) or updates an account. Not saved.</summary>
    Task<Guid> Stage(Guid? id, UserAccountInput input, CancellationToken ct);

    /// <summary>Locks or unlocks sign-in. Not saved.</summary>
    Task SetActive(Guid id, bool isActive, CancellationToken ct);

    /// <param name="ids">Only these accounts; null = every account.</param>
    Task<List<UserAccount>> List(IReadOnlyCollection<Guid>? ids, CancellationToken ct);
}
