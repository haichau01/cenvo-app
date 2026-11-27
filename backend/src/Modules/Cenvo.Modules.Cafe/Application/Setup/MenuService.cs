namespace Cenvo.Modules.Cafe.Application.Setup;

/// <summary>POS menu = finished-good products + CafeItem settings (topping, visibility, order).</summary>
public class MenuService(AppDbContext db)
{
    private const int UncategorizedSort = 999;

    public Task<List<MenuItemDto>> Menu(bool posOnly, CancellationToken ct)
    {
        var query =
            from p in db.Set<Product>().AsNoTracking()
            where p.Kind == ProductKind.FinishedGood && (!posOnly || p.IsActive)
            join ci in db.Set<CafeItem>() on p.Id equals ci.ProductId into settings
            from ci in settings.DefaultIfEmpty()
            where !posOnly || ci == null || ci.ShowOnPos
            // Sort on entity columns before projecting: EF cannot translate ordering by DTO record members.
            orderby p.Category == null ? UncategorizedSort : p.Category.SortOrder, ci == null ? 0 : ci.SortOrder, p.Name
            select new MenuItemDto(
                p.Id, p.Code, p.Name, p.CategoryId, p.Category!.Name,
                p.Category == null ? UncategorizedSort : p.Category.SortOrder,
                db.Set<ProductPrice>().Where(x => x.ProductId == p.Id).Select(x => (decimal?)x.Price).FirstOrDefault() ?? p.DefaultPrice,
                p.ImageUrl, ci != null && ci.IsTopping, ci == null || ci.ShowOnPos, ci == null ? 0 : ci.SortOrder,
                db.Set<RecipeLine>().Any(r => r.MenuProductId == p.Id));
        return query.ToListAsync(ct);
    }

    public async Task SaveSettings(Guid productId, MenuSettingsInput input, CancellationToken ct)
    {
        var item = await db.Set<CafeItem>().FindAsync([productId], ct);
        if (item == null) db.Add(item = new CafeItem { ProductId = productId });
        item.IsTopping = input.IsTopping;
        item.ShowOnPos = input.ShowOnPos;
        item.SortOrder = input.SortOrder;
        await db.SaveChangesAsync(ct);
    }
}
