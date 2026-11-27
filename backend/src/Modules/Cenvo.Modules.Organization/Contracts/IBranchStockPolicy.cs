namespace Cenvo.Modules.Organization.Contracts;

/// <summary>Branch stock policy — all Inventory needs to know, rather than the whole BranchService.</summary>
public interface IBranchStockPolicy
{
    Task<bool> AllowNegativeStock(Guid branchId, CancellationToken ct);
}
