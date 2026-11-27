namespace Cenvo.Modules.Inventory.Domain;

public class Supplier : BaseEntity, ISoftDelete
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public bool IsDeleted { get; set; }
}
