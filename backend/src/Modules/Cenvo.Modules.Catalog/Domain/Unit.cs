namespace Cenvo.Modules.Catalog.Domain;

public class Unit : BaseEntity, ISoftDelete
{
    public string Name { get; set; } = "";
    public bool IsDeleted { get; set; }
}
