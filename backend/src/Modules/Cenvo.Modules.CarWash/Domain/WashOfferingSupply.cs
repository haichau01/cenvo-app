namespace Cenvo.Modules.CarWash.Domain;

/// <summary>Supply (chemical, towel...) used per vehicle for a service. Quantity is in <see cref="UnitId"/> (null = base unit).</summary>
public class WashOfferingSupply
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfferingId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? UnitId { get; set; }
    [Precision(18, 3)] public decimal Quantity { get; set; }
}
