namespace Cenvo.Modules.Cafe.Domain;

/// <summary>Recipe (BOM): 1 serving of [item] needs Quantity [UnitId] of [ingredient].</summary>
public class RecipeLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuProductId { get; set; }
    public Guid IngredientProductId { get; set; }
    public Guid? UnitId { get; set; }
    [Precision(18, 3)] public decimal Quantity { get; set; }
}
