namespace Cenvo.Modules.Cafe.Application.Setup;

/// <summary>Recipe (ingredient quantities) of each menu item.</summary>
public class RecipeService(AppDbContext db, IUnitConverterFactory converters)
{
    public async Task<List<RecipeLineDto>> Get(Guid productId, CancellationToken ct)
    {
        var lines = await db.Set<RecipeLine>().AsNoTracking().Where(r => r.MenuProductId == productId).ToListAsync(ct);
        var ingredientIds = lines.Select(l => l.IngredientProductId).Distinct().ToList();
        var ingredients = await db.Set<Product>().IgnoreQueryFilters() // discontinued ingredients still show in old recipes
            .Where(p => ingredientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var unitIds = lines.Select(l => l.UnitId ?? ingredients[l.IngredientProductId].BaseUnitId).Distinct().ToList();
        var unitNames = await db.Set<Unit>().IgnoreQueryFilters().Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return lines.Select(l =>
        {
            var ingredient = ingredients[l.IngredientProductId];
            return new RecipeLineDto(ingredient.Id, ingredient.Code, ingredient.Name, l.UnitId,
                unitNames.GetValueOrDefault(l.UnitId ?? ingredient.BaseUnitId, ""), l.Quantity);
        }).OrderBy(l => l.IngredientCode).ToList();
    }

    public async Task Save(Guid productId, RecipeInput[] lines, CancellationToken ct)
    {
        if (lines.Any(l => l.Quantity <= 0)) throw new BusinessException(CafeErrors.RecipeQuantityMustBePositive);
        if (lines.Any(l => l.IngredientProductId == productId)) throw new BusinessException(CafeErrors.RecipeSelfReference);

        // Validate unit conversion on save so errors don't surface only at checkout.
        var converter = await converters.Create(lines.Select(l => l.IngredientProductId), ct);
        foreach (var line in lines) converter.ToBase(line.IngredientProductId, line.UnitId, line.Quantity);

        var existing = await db.Set<RecipeLine>().Where(r => r.MenuProductId == productId).ToListAsync(ct);
        db.RemoveRange(existing);
        db.AddRange(lines.Select(l => new RecipeLine
        {
            MenuProductId = productId, IngredientProductId = l.IngredientProductId, UnitId = l.UnitId, Quantity = l.Quantity,
        }));
        await db.SaveChangesAsync(ct);
    }
}
