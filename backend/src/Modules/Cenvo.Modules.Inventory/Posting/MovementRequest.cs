namespace Cenvo.Modules.Inventory.Posting;

/// <param name="Quantity">Base unit: + in, − out.</param>
/// <param name="UnitCost">Base-unit cost; null = use the current moving-average cost.</param>
public record MovementRequest(Guid WarehouseId, Guid ProductId, decimal Quantity, decimal? UnitCost = null);

/// <summary>Negative-stock warning after posting (only when negative stock is allowed).</summary>
public record StockWarning(Guid ProductId, Guid WarehouseId, decimal BalanceAfter)
{
    public static List<StockWarning> From(IEnumerable<StockMovement> movements) =>
        movements.Where(m => m.BalanceAfter < 0).Select(m => new StockWarning(m.ProductId, m.WarehouseId, m.BalanceAfter)).ToList();
}
