namespace Cenvo.Modules.Schedule.Domain;

/// <summary>A shift a branch runs every day, e.g. Sáng 06:00–12:00, with the minimum people per department.</summary>
public class ShiftTemplate : BaseEntity, IBranchScoped, ISoftDelete
{
    public static readonly TimeSpan MaxLength = TimeSpan.FromHours(16);
    public const int MaxStaff = 50;

    public Guid BranchId { get; set; }
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    /// <summary>Not after <see cref="StartTime"/> = ends the next day (overnight shift).</summary>
    public TimeOnly EndTime { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public List<ShiftRequirement> Requirements { get; set; } = [];

    public void Update(string? name, TimeOnly start, TimeOnly end, int sortOrder, bool isActive, IReadOnlyCollection<StaffingNeed> needs)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException(ScheduleErrors.TemplateNameRequired);
        if (start == end) throw new BusinessException(ScheduleErrors.TemplateZeroLength);
        if (ShiftWindow.Length(start, end) > MaxLength) throw new BusinessException(ScheduleErrors.TemplateTooLong);
        if (needs.Any(n => n.MinStaff is < 0 or > MaxStaff)) throw new BusinessException(ScheduleErrors.InvalidMinStaff);

        Name = name.Trim();
        StartTime = start;
        EndTime = end;
        SortOrder = sortOrder;
        IsActive = isActive;
        SetNeeds(needs.Where(n => n.MinStaff > 0).DistinctBy(n => n.Department).ToList());
    }

    /// <summary>Updated in place (key = template + department), so EF never sees a removed and re-added row.</summary>
    private void SetNeeds(List<StaffingNeed> wanted)
    {
        Requirements.RemoveAll(r => wanted.All(n => n.Department != r.Department));
        foreach (var need in wanted)
        {
            var existing = Requirements.FirstOrDefault(r => r.Department == need.Department);
            if (existing != null) existing.MinStaff = need.MinStaff;
            else Requirements.Add(new ShiftRequirement { ShiftTemplateId = Id, Department = need.Department, MinStaff = need.MinStaff });
        }
    }

    public IReadOnlyList<StaffingNeed> Needs() => Requirements.Select(r => new StaffingNeed(r.Department, r.MinStaff)).ToList();
}
