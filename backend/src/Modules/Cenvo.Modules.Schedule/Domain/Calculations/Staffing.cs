namespace Cenvo.Modules.Schedule.Domain.Calculations;

public record StaffingNeed(Department Department, int MinStaff);

public record Shortfall(Department Department, int Required, int Assigned);

public static class Staffing
{
    /// <summary>Departments of a shift with fewer people than required. Staff without a department count as General.</summary>
    public static List<Shortfall> Shortfalls(IEnumerable<StaffingNeed> needs, IEnumerable<Department?> assignedDepartments)
    {
        var counts = assignedDepartments.GroupBy(d => d ?? Department.General).ToDictionary(g => g.Key, g => g.Count());
        return needs
            .Where(n => n.MinStaff > counts.GetValueOrDefault(n.Department))
            .Select(n => new Shortfall(n.Department, n.MinStaff, counts.GetValueOrDefault(n.Department)))
            .ToList();
    }
}
