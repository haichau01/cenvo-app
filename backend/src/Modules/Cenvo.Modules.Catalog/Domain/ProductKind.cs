namespace Cenvo.Modules.Catalog.Domain;

public enum ProductKind
{
    /// <summary>Ingredient (coffee beans, milk, sugar...).</summary>
    Ingredient = 1,
    /// <summary>Finished good / menu item (cup of coffee, topping).</summary>
    FinishedGood = 2,
    /// <summary>Merchandise for resale (home appliances — phase 3).</summary>
    Merchandise = 3,
    /// <summary>Supplies / chemicals (car wash — phase 2).</summary>
    Supply = 4,
}
