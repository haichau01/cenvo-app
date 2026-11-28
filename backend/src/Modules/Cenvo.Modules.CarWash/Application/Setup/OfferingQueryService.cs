namespace Cenvo.Modules.CarWash.Application.Setup;

public class OfferingQueryService(AppDbContext db)
{
    /// <param name="includeInactive">Managers see every service; intake only sees the active ones.</param>
    public Task<List<OfferingDto>> List(bool includeInactive, CancellationToken ct) =>
        db.Set<WashOffering>().AsNoTracking()
            .Where(offering => includeInactive || offering.IsActive)
            .OrderBy(offering => offering.SortOrder).ThenBy(offering => offering.Name)
            .Take(Limits.MaxListSize)
            .Select(offering => new OfferingDto
            {
                Id = offering.Id,
                Code = offering.Code,
                Name = offering.Name,
                DurationMinutes = offering.DurationMinutes,
                CommissionType = offering.CommissionType,
                CommissionValue = offering.CommissionValue,
                SortOrder = offering.SortOrder,
                IsActive = offering.IsActive,
                Prices = offering.Prices.OrderBy(price => price.VehicleType)
                    .Select(price => new OfferingPriceDto(price.VehicleType, price.Price)).ToList(),
                Supplies = (from supply in offering.Supplies
                            join product in db.Set<Product>() on supply.ProductId equals product.Id
                            join unit in db.Set<Unit>() on (supply.UnitId ?? product.BaseUnitId) equals unit.Id
                            select new OfferingSupplyDto(supply.ProductId, product.Name, supply.UnitId, unit.Name, supply.Quantity))
                    .ToList(),
            })
            .ToListAsync(ct);
}
