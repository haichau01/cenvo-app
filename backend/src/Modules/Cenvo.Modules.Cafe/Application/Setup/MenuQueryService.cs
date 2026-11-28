using Cenvo.Modules.Catalog.Application.Products;

namespace Cenvo.Modules.Cafe.Application.Setup;

/// <summary>Reads the menu: finished goods + branch selling price + POS settings.</summary>
public class MenuQueryService(AppDbContext db)
{
    /// <summary>Categories without a sort order (or no category at all) go last.</summary>
    private const int UncategorizedSort = 999;

    /// <summary>What the cashier sees on the POS: active items not hidden by the manager.</summary>
    public Task<List<MenuItemDto>> PosMenu(CancellationToken ct) =>
        ToSortedDtos(MenuRows().Where(row => row.Product.IsActive && (row.Settings == null || row.Settings.ShowOnPos)), ct);

    /// <summary>Everything, for the manager's menu setup screen.</summary>
    public Task<List<MenuItemDto>> FullMenu(CancellationToken ct) => ToSortedDtos(MenuRows(), ct);

    /// <summary>One row per finished good. <c>Settings</c> is null when the product has no POS settings yet.</summary>
    private IQueryable<MenuRow> MenuRows()
    {
        var settings = db.Set<CafeItem>();
        var recipes = db.Set<RecipeLine>();
        return db.Set<Product>().AsNoTracking()
            .FinishedGoods()
            .WithSellingPrice(db)
            .Select(x => new MenuRow
            {
                Product = x.Product,
                Price = x.Price,
                Settings = settings.FirstOrDefault(s => s.ProductId == x.Product.Id),
                HasRecipe = recipes.Any(r => r.MenuProductId == x.Product.Id),
            });
    }

    private static Task<List<MenuItemDto>> ToSortedDtos(IQueryable<MenuRow> rows, CancellationToken ct) =>
        rows.OrderBy(row => row.Product.Category == null ? UncategorizedSort : row.Product.Category.SortOrder)
            .ThenBy(row => row.Settings == null ? CafeItem.DefaultSortOrder : row.Settings.SortOrder)
            .ThenBy(row => row.Product.Name)
            .Select(row => new MenuItemDto
            {
                ProductId = row.Product.Id,
                Code = row.Product.Code,
                Name = row.Product.Name,
                CategoryId = row.Product.CategoryId,
                CategoryName = row.Product.Category == null ? null : row.Product.Category.Name,
                CategorySort = row.Product.Category == null ? UncategorizedSort : row.Product.Category.SortOrder,
                Price = row.Price,
                ImageUrl = row.Product.ImageUrl,
                IsTopping = row.Settings != null && row.Settings.IsTopping,
                ShowOnPos = row.Settings == null || row.Settings.ShowOnPos,
                SortOrder = row.Settings == null ? CafeItem.DefaultSortOrder : row.Settings.SortOrder,
                HasRecipe = row.HasRecipe,
            })
            .ToListAsync(ct);

    private class MenuRow
    {
        public required Product Product { get; init; }
        public decimal Price { get; init; }
        public CafeItem? Settings { get; init; }
        public bool HasRecipe { get; init; }
    }
}
