namespace Cenvo.Modules.Catalog.Application.Products;

public class ProductService(AppDbContext db, IProductUsageChecker usage)
{
    public async Task<Guid> Save(Guid? id, ProductInput input, CancellationToken ct)
    {
        var code = input.Code.Trim();
        if (await db.Set<Product>().AnyAsync(p => p.Code == code && p.Id != id, ct))
            throw new BusinessException(CatalogErrors.DuplicateCode(code));

        var product = id == null ? db.Add(new Product()).Entity : await LoadWithUnits(id.Value, ct);
        if (id != null && product.BaseUnitId != input.BaseUnitId && await usage.HasStockMovements(product.Id, ct))
            throw new BusinessException(CatalogErrors.BaseUnitLocked);

        product.Code = code;
        product.Name = input.Name.Trim();
        product.Kind = input.Kind;
        product.CategoryId = input.CategoryId;
        product.BaseUnitId = input.BaseUnitId;
        product.DefaultPrice = input.DefaultPrice;
        product.MinStock = input.MinStock;
        product.TrackStock = input.TrackStock;
        product.IsActive = input.IsActive;
        product.ImageUrl = input.ImageUrl;
        product.SetUnits((input.Units ?? []).Select(u => (u.UnitId, u.Factor)));

        await db.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        db.Remove(await db.Set<Product>().FindAsync([id], ct) ?? throw new NotFoundException("sản phẩm", id));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Sets a branch-specific price for the current branch; null = fall back to the default price.</summary>
    public async Task SetBranchPrice(Guid productId, decimal? price, CancellationToken ct)
    {
        var existing = await db.Set<ProductPrice>().FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        switch (existing, price)
        {
            case (null, null): return;
            case (not null, null): db.Remove(existing); break;
            case (not null, not null): existing.Price = price.Value; break;
            case (null, not null): db.Add(new ProductPrice { ProductId = productId, Price = price.Value }); break;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Product> LoadWithUnits(Guid id, CancellationToken ct) =>
        await db.Set<Product>().Include(x => x.Units).FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new NotFoundException("sản phẩm", id);
}
