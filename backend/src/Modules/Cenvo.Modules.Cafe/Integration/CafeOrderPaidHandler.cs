namespace Cenvo.Modules.Cafe.Integration;

/// <summary>One cup sold → inventory deducts ingredients by recipe (same transaction as the order).</summary>
public class CafeOrderPaidHandler(AppDbContext db, IUnitConverterFactory converters, ISaleStockIssuer stockIssuer)
    : IDomainEventHandler<CafeOrderPaid>
{
    public async Task Handle(CafeOrderPaid e, CancellationToken ct)
    {
        var soldIds = e.Items.Select(i => i.ProductId).Distinct().ToList();
        var recipes = (await db.Set<RecipeLine>().AsNoTracking().Where(r => soldIds.Contains(r.MenuProductId)).ToListAsync(ct))
            .GroupBy(r => r.MenuProductId)
            .ToDictionary(g => g.Key, g => g.Select(r => new RecipeItem(r.IngredientProductId, r.UnitId, r.Quantity)).ToList());

        var ingredientIds = recipes.Values.SelectMany(r => r.Select(i => i.IngredientProductId));
        var converter = await converters.Create(soldIds.Concat(ingredientIds), ct);

        var issueLines = RecipeExpander.Expand(e.Items, recipes, converter);
        await stockIssuer.IssueForSale(e.BranchId, CafeOrder.StockRefType, e.OrderId, e.Code, issueLines, ct);
    }
}
