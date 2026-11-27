namespace Cenvo.Modules.Cafe.Domain;

public class Area : BaseEntity, IBranchScoped, ISoftDelete
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }

    public void Update(string name, int sortOrder)
    {
        Name = name.Trim();
        SortOrder = sortOrder;
    }
}
