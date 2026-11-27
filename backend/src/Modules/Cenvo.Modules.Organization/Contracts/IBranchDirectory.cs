namespace Cenvo.Modules.Organization.Contracts;

/// <summary>Name and contact details printed on receipts and documents.</summary>
public record BranchContact(string Name, string? Address, string? Phone);

/// <summary>Branch contact details — all other modules need to know, rather than the whole BranchService.</summary>
public interface IBranchDirectory
{
    Task<BranchContact?> Contact(Guid branchId, CancellationToken ct);
}
