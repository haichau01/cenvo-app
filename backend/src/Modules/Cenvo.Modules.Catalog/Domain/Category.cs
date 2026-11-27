namespace Cenvo.Modules.Catalog.Domain;

public class Category : BaseEntity, ISoftDelete
{
    public string Name { get; set; } = "";
    public ProductKind Kind { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }

    public void Update(string name, ProductKind kind, int sortOrder)
    {
        Name = name.Trim();
        Kind = kind;
        SortOrder = sortOrder;
    }
}
