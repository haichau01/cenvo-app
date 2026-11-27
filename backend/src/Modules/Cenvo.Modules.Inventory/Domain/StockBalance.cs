namespace Cenvo.Modules.Inventory.Domain;

/// <summary>Aggregated stock per (warehouse, product) — only StockService updates it, in the same transaction as StockMovement.</summary>
public class StockBalance : IBranchScoped
{
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    [Precision(18, 3)] public decimal Quantity { get; set; }
    /// <summary>Moving weighted-average cost.</summary>
    [Precision(18, 4)] public decimal AverageCost { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>Optimistic concurrency token (PostgreSQL xmin).</summary>
    [System.ComponentModel.DataAnnotations.Timestamp] public uint Version { get; set; }

    public void Receive(decimal quantity, decimal unitCost)
    {
        AverageCost = CostCalculator.MovingAverage(Quantity, AverageCost, quantity, unitCost);
        Quantity += quantity;
    }

    public void Issue(decimal quantity) => Quantity -= quantity;

    /// <summary>Reverses a receipt: removes exactly the received value so the cost returns to what it was.</summary>
    public void ReverseReceipt(decimal quantity, decimal unitCost)
    {
        var after = Quantity - quantity;
        if (after > 0) AverageCost = Math.Max(0, Math.Round((Quantity * AverageCost - quantity * unitCost) / after, 4));
        Quantity = after;
    }
}
