namespace Cenvo.Modules.Cafe.Domain;

/// <summary>Topping on an order line — Quantity is servings PER cup.</summary>
public class CafeOrderLineTopping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LineId { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal UnitPrice { get; set; }
    [Precision(18, 3)] public decimal Quantity { get; set; } = 1;

    public static CafeOrderLineTopping From(MenuPrice price, decimal quantity) =>
        new() { ProductId = price.ProductId, Name = price.Name, UnitPrice = price.Price, Quantity = quantity };
}
