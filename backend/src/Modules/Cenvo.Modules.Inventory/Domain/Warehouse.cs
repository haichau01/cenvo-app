namespace Cenvo.Modules.Inventory.Domain;

public class Warehouse : BaseEntity, IBranchScoped, ISoftDelete
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Default warehouse of the branch — sales deduct stock here.</summary>
    public bool IsDefault { get; set; }
    public bool IsDeleted { get; set; }
}
