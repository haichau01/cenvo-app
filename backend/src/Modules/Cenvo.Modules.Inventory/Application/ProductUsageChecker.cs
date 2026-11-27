namespace Cenvo.Modules.Inventory.Application;

public class ProductUsageChecker(AppDbContext db) : IProductUsageChecker
{
    public Task<bool> HasStockMovements(Guid productId, CancellationToken ct) =>
        db.Set<StockMovement>().IgnoreQueryFilters() // across all branches
            .AnyAsync(m => m.ProductId == productId, ct);
}
