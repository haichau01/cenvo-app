namespace Cenvo.Modules.Catalog.Domain;

public class Product : BaseEntity, ISoftDelete
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public ProductKind Kind { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    /// <summary>Base unit: stock is always counted in this unit.</summary>
    public Guid BaseUnitId { get; set; }
    public Unit? BaseUnit { get; set; }
    /// <summary>Default selling price (a branch can override it with ProductPrice).</summary>
    public decimal DefaultPrice { get; set; }
    /// <summary>Minimum stock level (base unit) — alerts when below.</summary>
    [Precision(18, 3)] public decimal MinStock { get; set; }
    /// <summary>Whether stock is tracked. Prepared drinks are not — inventory deducts by recipe.</summary>
    public bool TrackStock { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public bool IsDeleted { get; set; }
    public List<ProductUnit> Units { get; set; } = [];

    /// <summary>Replaces the conversion units (skips the base unit and factors ≤ 0).</summary>
    public void SetUnits(IEnumerable<(Guid UnitId, decimal Factor)> units)
    {
        var wanted = units.Where(u => u.UnitId != BaseUnitId && u.Factor > 0).ToList();
        Units.RemoveAll(u => wanted.All(w => w.UnitId != u.UnitId));
        foreach (var (unitId, factor) in wanted)
        {
            var existing = Units.FirstOrDefault(u => u.UnitId == unitId);
            if (existing != null) existing.Factor = factor;
            else Units.Add(new ProductUnit { ProductId = Id, UnitId = unitId, Factor = factor });
        }
    }
}
