namespace Cenvo.Modules.Cafe.Domain;

/// <summary>
/// POS settings for a finished-good product (menu item or topping).
/// A product without a CafeItem row uses the defaults: shown on POS, not a topping, <see cref="DefaultSortOrder"/>.
/// </summary>
public class CafeItem
{
    public const int DefaultSortOrder = 0;

    public Guid ProductId { get; private set; }
    public bool IsTopping { get; private set; }
    public bool ShowOnPos { get; private set; } = true;
    public int SortOrder { get; private set; } = DefaultSortOrder;

    private CafeItem() { } // EF

    public static CafeItem For(Guid productId) => new() { ProductId = productId };

    public static CafeItem Topping(Guid productId, int sortOrder) => new() { ProductId = productId, IsTopping = true, SortOrder = sortOrder };

    public void Configure(bool isTopping, bool showOnPos, int sortOrder)
    {
        IsTopping = isTopping;
        ShowOnPos = showOnPos;
        SortOrder = sortOrder;
    }
}
