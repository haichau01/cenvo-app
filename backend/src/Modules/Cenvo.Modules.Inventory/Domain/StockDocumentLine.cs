namespace Cenvo.Modules.Inventory.Domain;

public class StockDocumentLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid ProductId { get; set; }
    /// <summary>Entry unit (null = base unit).</summary>
    public Guid? UnitId { get; set; }
    /// <summary>Quantity in the entry unit. For stocktakes: the quantity actually counted.</summary>
    [Precision(18, 3)] public decimal Quantity { get; set; }
    /// <summary>Quantity converted to the base unit.</summary>
    [Precision(18, 3)] public decimal BaseQuantity { get; set; }
    /// <summary>Stocktake: book quantity at posting time (base unit).</summary>
    [Precision(18, 3)] public decimal? SystemQuantity { get; set; }
    /// <summary>Unit price in the entry unit. Sale issues: base-unit cost.</summary>
    [Precision(18, 4)] public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    /// <summary>Base-unit cost of a receipt line.</summary>
    public decimal BaseUnitCost => BaseQuantity == 0 ? 0 : Amount / BaseQuantity;

    public static StockDocumentLine Create(Guid productId, Guid? unitId, decimal quantity, decimal baseQuantity,
        decimal unitPrice, string? note) => new()
    {
        ProductId = productId, UnitId = unitId, Quantity = quantity, BaseQuantity = baseQuantity,
        UnitPrice = unitPrice, Amount = Math.Round(quantity * unitPrice, 2), Note = note,
    };

    public void SetCost(decimal baseUnitCost)
    {
        UnitPrice = baseUnitCost;
        Amount = Math.Round(baseUnitCost * BaseQuantity, 2);
    }
}
