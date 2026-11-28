namespace Cenvo.Modules.CarWash.Domain;

/// <summary>A car, identified by its normalized licence plate. Shared by every branch.</summary>
public class Vehicle : BaseEntity, ISoftDelete
{
    /// <summary>Normalized (see <see cref="PlateNumber"/>); unique among vehicles that are not deleted.</summary>
    public string Plate { get; set; } = "";
    public VehicleType VehicleType { get; set; }
    /// <summary>Make / model / colour as the staff noted it, e.g. "Vios trắng".</summary>
    public string? Description { get; set; }
    public Guid? CustomerId { get; set; }
    public bool IsDeleted { get; set; }

    public static Vehicle Register(string normalizedPlate, VehicleType vehicleType) =>
        new() { Plate = normalizedPlate, VehicleType = vehicleType };

    /// <summary>Keeps what staff saw at intake: type, description and current owner (when known).</summary>
    public void UpdateAtIntake(VehicleType vehicleType, string? description, Guid? customerId)
    {
        VehicleType = vehicleType;
        if (!string.IsNullOrWhiteSpace(description)) Description = description.Trim();
        if (customerId != null) CustomerId = customerId;
    }
}
