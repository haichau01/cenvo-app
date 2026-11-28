namespace Cenvo.Modules.Organization.Contracts;

public record ManagerApproval(Guid UserId, string FullName);

/// <summary>
/// A manager approves an action on the cashier's device by typing their own user name and password
/// (e.g. a discount above the cashier's limit). The calling endpoint must be rate-limited.
/// </summary>
public interface IManagerApproval
{
    /// <summary>Throws a BusinessException when the credentials are wrong or the person does not manage this branch.</summary>
    Task<ManagerApproval> Approve(string userName, string password, Guid branchId, CancellationToken ct);
}
