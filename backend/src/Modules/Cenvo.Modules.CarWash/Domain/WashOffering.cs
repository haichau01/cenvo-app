namespace Cenvo.Modules.CarWash.Domain;

/// <summary>A car wash service on the price list (rửa bọt tuyết, hút bụi, phủ ceramic...). Chain-wide, priced per vehicle type.</summary>
public class WashOffering : BaseEntity, ISoftDelete
{
    public const int MaxDurationMinutes = 600;

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Usual time to perform the service — used for the estimated waiting time.</summary>
    public int DurationMinutes { get; set; }
    public CommissionType CommissionType { get; set; } = CommissionType.Percent;
    /// <summary>Percent (0–100) or VND per vehicle, depending on <see cref="CommissionType"/>.</summary>
    public decimal CommissionValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public List<WashOfferingPrice> Prices { get; set; } = [];
    public List<WashOfferingSupply> Supplies { get; set; } = [];

    public void Update(string code, string name, int durationMinutes, CommissionType commissionType, decimal commissionValue,
        int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException(CarWashErrors.ServiceNameRequired);
        if (durationMinutes is < 1 or > MaxDurationMinutes) throw new BusinessException(CarWashErrors.InvalidDuration);
        if (commissionValue < 0 || (commissionType == CommissionType.Percent && commissionValue > 100))
            throw new BusinessException(CarWashErrors.InvalidCommission);

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        DurationMinutes = durationMinutes;
        CommissionType = commissionType;
        CommissionValue = commissionValue;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public void SetPrices(IEnumerable<(VehicleType VehicleType, decimal Price)> prices)
    {
        var wanted = prices.GroupBy(p => p.VehicleType).Select(g => g.Last()).ToList();
        if (wanted.Count == 0) throw new BusinessException(CarWashErrors.ServicePriceRequired);
        if (wanted.Any(p => p.Price < 0)) throw new BusinessException(CarWashErrors.InvalidPrice);

        Prices.RemoveAll(p => wanted.All(w => w.VehicleType != p.VehicleType));
        foreach (var (vehicleType, price) in wanted)
        {
            var existing = Prices.FirstOrDefault(p => p.VehicleType == vehicleType);
            if (existing != null) existing.Price = price;
            else Prices.Add(new WashOfferingPrice { OfferingId = Id, VehicleType = vehicleType, Price = price });
        }
    }

    /// <summary>Supplies used per vehicle (deducted from stock when the wash is completed).</summary>
    public void SetSupplies(IEnumerable<(Guid ProductId, Guid? UnitId, decimal Quantity)> supplies)
    {
        var wanted = supplies.ToList();
        if (wanted.Any(s => s.Quantity <= 0)) throw new BusinessException(CarWashErrors.SupplyQuantityMustBePositive);
        Supplies.Clear();
        Supplies.AddRange(wanted.Select(s => new WashOfferingSupply
        {
            OfferingId = Id, ProductId = s.ProductId, UnitId = s.UnitId, Quantity = s.Quantity,
        }));
    }

    public decimal PriceFor(VehicleType vehicleType) =>
        Prices.FirstOrDefault(p => p.VehicleType == vehicleType)?.Price
        ?? throw new BusinessException(CarWashErrors.NoPriceForVehicle(Name));
}
