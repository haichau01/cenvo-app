namespace Cenvo.Modules.Catalog.Application.Products;

public class ProductQueryService(AppDbContext db)
{
    public Task<List<ProductDto>> List(ProductKind? kind, string? search, CancellationToken ct)
    {
        var query = db.Set<Product>().AsNoTracking().Where(p => kind == null || p.Kind == kind);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%") || EF.Functions.ILike(p.Code, $"%{search}%"));

        // ProductPrice is already limited to the current branch by the global filter.
        return query.OrderBy(p => p.Code).Take(Limits.MaxListSize).Select(p => new ProductDto(
            p.Id, p.Code, p.Name, p.Kind, p.CategoryId, p.Category!.Name, p.BaseUnitId, p.BaseUnit!.Name, p.DefaultPrice,
            db.Set<ProductPrice>().Where(x => x.ProductId == p.Id).Select(x => (decimal?)x.Price).FirstOrDefault() ?? p.DefaultPrice,
            p.MinStock, p.TrackStock, p.IsActive, p.ImageUrl,
            p.Units.Select(u => new ProductUnitDto(u.UnitId, u.Unit!.Name, u.Factor)).ToArray()
        )).ToListAsync(ct);
    }
}
