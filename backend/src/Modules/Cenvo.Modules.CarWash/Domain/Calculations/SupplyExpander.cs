namespace Cenvo.Modules.CarWash.Domain.Calculations;

public record SupplyUsage(Guid ProductId, Guid? UnitId, decimal Quantity);

/// <summary>Services washed → supply quantities in the base unit (one issue line per product). Pure function.</summary>
public static class SupplyExpander
{
    public static List<SaleIssueLine> Expand(IEnumerable<Guid> serviceIds,
        IReadOnlyDictionary<Guid, List<SupplyUsage>> suppliesByService, UnitConverter converter)
    {
        var totals = new Dictionary<Guid, decimal>();
        foreach (var serviceId in serviceIds)
        {
            if (!suppliesByService.TryGetValue(serviceId, out var supplies)) continue;
            foreach (var supply in supplies)
                totals[supply.ProductId] = totals.GetValueOrDefault(supply.ProductId)
                                           + converter.ToBase(supply.ProductId, supply.UnitId, supply.Quantity);
        }
        return totals.Where(t => t.Value != 0).Select(t => new SaleIssueLine(t.Key, t.Value)).ToList();
    }
}
