namespace Cenvo.Modules.Catalog.Application;

public class UnitConverterFactory(AppDbContext db) : IUnitConverterFactory
{
    public async Task<UnitConverter> Create(IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToList();
        var products = await db.Set<Product>()
            .IgnoreQueryFilters() // soft-deleted products still need conversion when cancelling old documents
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.BaseUnitId, p.TrackStock, Units = p.Units.Select(u => new { u.UnitId, u.Factor }) })
            .ToListAsync(ct);

        return new UnitConverter(products.ToDictionary(p => p.Id, p => new UnitConverter.Info(
            p.Name, p.BaseUnitId, p.TrackStock, p.Units.ToDictionary(u => u.UnitId, u => u.Factor))));
    }
}
