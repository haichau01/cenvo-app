namespace Cenvo.Modules.CarWash.Domain;

public class WashOfferingPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfferingId { get; set; }
    public VehicleType VehicleType { get; set; }
    public decimal Price { get; set; }
}
