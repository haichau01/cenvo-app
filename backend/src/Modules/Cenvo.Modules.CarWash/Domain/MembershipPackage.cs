namespace Cenvo.Modules.CarWash.Domain;

/// <summary>Prepaid wash package for sale, e.g. "Mua 10 tặng 2": 12 washes of one service, valid for a number of days.</summary>
public class MembershipPackage : BaseEntity, ISoftDelete
{
    public const int MaxUses = 100;
    public const int MaxValidDays = 1095;

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>The service each use covers.</summary>
    public Guid OfferingId { get; set; }
    /// <summary>Restricts the package to one vehicle type (prices differ per type); null = any type.</summary>
    public VehicleType? VehicleType { get; set; }
    public decimal Price { get; set; }
    /// <summary>Total uses including bonus ones (buy 10 get 2 → 12).</summary>
    public int Uses { get; set; }
    public int ValidDays { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public void Update(string code, string name, Guid offeringId, VehicleType? vehicleType, decimal price, int uses, int validDays,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException(CarWashErrors.PackageNameRequired);
        if (price < 0) throw new BusinessException(CarWashErrors.InvalidPrice);
        if (uses is < 1 or > MaxUses) throw new BusinessException(CarWashErrors.InvalidPackageUses);
        if (validDays is < 1 or > MaxValidDays) throw new BusinessException(CarWashErrors.InvalidPackageValidity);

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        OfferingId = offeringId;
        VehicleType = vehicleType;
        Price = price;
        Uses = uses;
        ValidDays = validDays;
        IsActive = isActive;
    }
}
