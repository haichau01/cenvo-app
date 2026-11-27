namespace Cenvo.Modules.Catalog.Application.Products;

public class ProductQueryService(AppDbContext db)
{
    public Task<List<ProductDto>> List(ProductKind? kind, string? search, CancellationToken ct)
    {
        var products = db.Set<Product>().AsNoTracking().Matching(search);
        if (kind != null) products = products.Where(p => p.Kind == kind);

        return products
            .OrderBy(p => p.Code)
            .Take(Limits.MaxListSize)
            .WithSellingPrice(db)
            .Select(x => new ProductDto
            {
                Id = x.Product.Id,
                Code = x.Product.Code,
                Name = x.Product.Name,
                Kind = x.Product.Kind,
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category!.Name,
                BaseUnitId = x.Product.BaseUnitId,
                BaseUnitName = x.Product.BaseUnit!.Name,
                DefaultPrice = x.Product.DefaultPrice,
                Price = x.Price,
                MinStock = x.Product.MinStock,
                TrackStock = x.Product.TrackStock,
                IsActive = x.Product.IsActive,
                ImageUrl = x.Product.ImageUrl,
                Units = x.Product.Units.Select(u => new ProductUnitDto(u.UnitId, u.Unit!.Name, u.Factor)).ToArray(),
            })
            .ToListAsync(ct);
    }
}
