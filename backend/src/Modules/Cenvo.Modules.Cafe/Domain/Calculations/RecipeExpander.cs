namespace Cenvo.Modules.Cafe.Domain.Calculations;

public record RecipeItem(Guid IngredientProductId, Guid? UnitId, decimal Quantity);

/// <summary>Expands recipes: items sold → ingredient quantities (base unit). Pure function for unit testing.</summary>
public static class RecipeExpander
{
    /// <param name="recipes">Recipes per item. A stock-tracked item without a recipe (bottled water) deducts itself.</param>
    public static List<SaleIssueLine> Expand(IEnumerable<SoldItem> sold,
        IReadOnlyDictionary<Guid, List<RecipeItem>> recipes, UnitConverter converter)
    {
        var totals = new Dictionary<Guid, decimal>();
        foreach (var item in sold)
        {
            if (recipes.TryGetValue(item.ProductId, out var recipe) && recipe.Count > 0)
            {
                foreach (var ingredient in recipe)
                    Add(ingredient.IngredientProductId,
                        converter.ToBase(ingredient.IngredientProductId, ingredient.UnitId, ingredient.Quantity) * item.Quantity);
            }
            else if (converter.Product(item.ProductId).TrackStock)
            {
                Add(item.ProductId, item.Quantity);
            }
        }
        return totals.Where(t => t.Value != 0).Select(t => new SaleIssueLine(t.Key, t.Value)).ToList();

        void Add(Guid productId, decimal quantity) => totals[productId] = totals.GetValueOrDefault(productId) + quantity;
    }
}
