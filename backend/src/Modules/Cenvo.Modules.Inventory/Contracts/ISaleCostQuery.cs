namespace Cenvo.Modules.Inventory.Contracts;

/// <summary>Cost of goods issued for one source document (a ticket, an order).</summary>
public record SaleCost
{
    public required Guid RefId { get; init; }
    public required decimal Cost { get; init; }
}

/// <summary>
/// Cost of goods of sales sources, for their reports. Returns a query (not a list) so the caller can join it with its own
/// documents and aggregate in SQL. Branch-filtered like every stock document.
/// </summary>
public interface ISaleCostQuery
{
    /// <summary>One row per source document: total value of its still-posted sale issue documents (cancelled ones are reversed).</summary>
    IQueryable<SaleCost> PostedCosts(string refType);
}
