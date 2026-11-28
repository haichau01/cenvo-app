namespace Cenvo.Modules.Cafe.Application.Setup;

/// <summary>Changes POS settings of a menu item (topping, visibility, order). Reading the menu: <see cref="MenuQueryService"/>.</summary>
public class MenuService(AppDbContext db)
{
    public async Task SaveSettings(Guid productId, MenuSettingsInput input, CancellationToken ct)
    {
        var item = await db.Set<CafeItem>().FindAsync([productId], ct) ?? db.Add(CafeItem.For(productId)).Entity;
        item.Configure(input.IsTopping, input.ShowOnPos, input.SortOrder);
        await db.SaveChangesAsync(ct);
    }
}
