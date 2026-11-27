namespace Cenvo.Modules.Catalog.Domain;

/// <summary>Conversion unit: 1 [Unit] = Factor [base unit]. E.g. 1 kg = 1000 g.</summary>
public class ProductUnit
{
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    [Precision(18, 6)] public decimal Factor { get; set; }
}
