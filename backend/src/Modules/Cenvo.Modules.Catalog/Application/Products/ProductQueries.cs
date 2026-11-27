namespace Cenvo.Modules.Catalog.Application.Products;

/// <summary>A product with its selling price at the current branch — the result of <see cref="ProductQueries.WithSellingPrice"/>.</summary>
public class PricedProduct
{
    public required Product Product { get; init; }
    public decimal Price { get; init; }
}

/// <summary>
/// Reusable, named query steps for products (Query Object pattern). Each step is a plain IQueryable
/// extension, so EF still translates the whole chain into one SQL statement:
/// <c>db.Set&lt;Product&gt;().FinishedGoods().ActiveOnly().WithSellingPrice(db)</c>.
/// </summary>
public static class ProductQueries
{
    /// <summary>Items sold to customers (drinks, food, toppings) — not ingredients or supplies.</summary>
    public static IQueryable<Product> FinishedGoods(this IQueryable<Product> products) =>
        products.Where(p => p.Kind == ProductKind.FinishedGood);

    public static IQueryable<Product> ActiveOnly(this IQueryable<Product> products) =>
        products.Where(p => p.IsActive);

    /// <summary>Name or code contains <paramref name="text"/> (case-insensitive). Empty text = no filter.</summary>
    public static IQueryable<Product> Matching(this IQueryable<Product> products, string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? products
            : products.Where(p => EF.Functions.ILike(p.Name, $"%{text}%") || EF.Functions.ILike(p.Code, $"%{text}%"));

    /// <summary>
    /// Selling price rule, written once: the branch's own price (ProductPrice) if set, otherwise Product.DefaultPrice.
    /// ProductPrice is branch-scoped, so the global filter already limits it to the current branch.
    /// </summary>
    public static IQueryable<PricedProduct> WithSellingPrice(this IQueryable<Product> products, DbContext db)
    {
        var branchPrices = db.Set<ProductPrice>();
        return products.Select(p => new PricedProduct
        {
            Product = p,
            Price = branchPrices.Where(x => x.ProductId == p.Id).Select(x => (decimal?)x.Price).FirstOrDefault()
                    ?? p.DefaultPrice,
        });
    }
}
