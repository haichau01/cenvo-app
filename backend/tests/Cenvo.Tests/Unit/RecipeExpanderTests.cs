using Cenvo.Modules.Cafe.Domain;
using Cenvo.Modules.Cafe.Domain.Calculations;
using Cenvo.Modules.Catalog.Domain;

namespace Cenvo.Tests.Unit;

public class RecipeExpanderTests
{
    private static readonly Guid G = Guid.NewGuid(), Kg = Guid.NewGuid(), Cup = Guid.NewGuid(), Bottle = Guid.NewGuid();
    private static readonly Guid Coffee = Guid.NewGuid(), Sugar = Guid.NewGuid(), BlackCoffee = Guid.NewGuid(),
        Water = Guid.NewGuid(), Untracked = Guid.NewGuid();

    private static readonly UnitConverter Conv = new(new Dictionary<Guid, UnitConverter.Info>
    {
        [Coffee] = new("Cà phê", G, true, new Dictionary<Guid, decimal> { [Kg] = 1000 }),
        [Sugar] = new("Đường", G, true, new Dictionary<Guid, decimal>()),
        [BlackCoffee] = new("Cà phê đen", Cup, false, new Dictionary<Guid, decimal>()),
        [Water] = new("Nước suối", Bottle, true, new Dictionary<Guid, decimal>()),
        [Untracked] = new("Món không công thức", Cup, false, new Dictionary<Guid, decimal>()),
    });

    private static readonly Dictionary<Guid, List<RecipeItem>> Recipes = new()
    {
        [BlackCoffee] = [new(Coffee, Kg, 0.02m), new(Sugar, null, 10)],
    };

    [Fact]
    public void Expands_recipe_with_unit_conversion_and_merges_same_ingredient()
    {
        var lines = RecipeExpander.Expand([new SoldItem(BlackCoffee, 2), new SoldItem(BlackCoffee, 1)], Recipes, Conv);
        Assert.Equal(60m, lines.Single(l => l.ProductId == Coffee).BaseQuantity);
        Assert.Equal(30m, lines.Single(l => l.ProductId == Sugar).BaseQuantity);
    }

    [Fact]
    public void Tracked_item_without_recipe_issues_itself() =>
        Assert.Equal(3m, RecipeExpander.Expand([new SoldItem(Water, 3)], Recipes, Conv).Single(l => l.ProductId == Water).BaseQuantity);

    [Fact]
    public void Untracked_item_without_recipe_issues_nothing() =>
        Assert.Empty(RecipeExpander.Expand([new SoldItem(Untracked, 1)], Recipes, Conv));
}
