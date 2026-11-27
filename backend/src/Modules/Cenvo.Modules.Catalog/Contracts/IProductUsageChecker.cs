namespace Cenvo.Modules.Catalog.Contracts;

/// <summary>Has the product had any stock movement? Declared by Catalog, implemented by Inventory (dependency inversion).</summary>
public interface IProductUsageChecker
{
    Task<bool> HasStockMovements(Guid productId, CancellationToken ct);
}
