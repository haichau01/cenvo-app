namespace Cenvo.Modules.Catalog.Domain;

/// <summary>Branch-specific selling price (overrides Product.DefaultPrice).</summary>
public class ProductPrice : BaseEntity, IBranchScoped
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Price { get; set; }
}
