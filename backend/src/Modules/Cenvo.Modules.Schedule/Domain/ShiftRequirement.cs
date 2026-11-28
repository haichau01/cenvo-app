namespace Cenvo.Modules.Schedule.Domain;

/// <summary>Minimum people of one department on a shift (e.g. 2 car wash staff on the morning shift).</summary>
public class ShiftRequirement
{
    public Guid ShiftTemplateId { get; set; }
    public Department Department { get; set; }
    public int MinStaff { get; set; }
}
