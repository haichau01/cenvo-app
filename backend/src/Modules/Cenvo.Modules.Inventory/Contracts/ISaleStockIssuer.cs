namespace Cenvo.Modules.Inventory.Contracts;

public record SaleIssueLine(Guid ProductId, decimal BaseQuantity);

/// <summary>
/// Stock issue contract for every sales source (cafe POS, car wash, appliance sales).
/// Does not call SaveChanges — runs inside the source document's transaction (domain event handler).
/// </summary>
public interface ISaleStockIssuer
{
    Task IssueForSale(Guid branchId, string refType, Guid refId, string refCode, IEnumerable<SaleIssueLine> lines, CancellationToken ct);

    /// <summary>Cancels the sale issue for a source document → reversal entries, stock returned.</summary>
    Task CancelByRef(string refType, Guid refId, string reason, CancellationToken ct);
}
