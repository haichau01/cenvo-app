namespace Cenvo.Modules.Cafe.Domain;

/// <summary>POS settings for a finished-good product (menu item or topping).</summary>
public class CafeItem
{
    public Guid ProductId { get; set; }
    public bool IsTopping { get; set; }
    public bool ShowOnPos { get; set; } = true;
    public int SortOrder { get; set; }
}
