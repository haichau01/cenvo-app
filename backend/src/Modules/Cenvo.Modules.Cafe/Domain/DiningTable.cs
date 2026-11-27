namespace Cenvo.Modules.Cafe.Domain;

public class DiningTable : BaseEntity, IBranchScoped, ISoftDelete
{
    public Guid BranchId { get; set; }
    public Guid AreaId { get; set; }
    public string Name { get; set; } = "";
    public int Seats { get; set; } = 4;
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }

    public void Update(Guid areaId, string name, int seats, int sortOrder)
    {
        AreaId = areaId;
        Name = name.Trim();
        Seats = seats;
        SortOrder = sortOrder;
    }
}
