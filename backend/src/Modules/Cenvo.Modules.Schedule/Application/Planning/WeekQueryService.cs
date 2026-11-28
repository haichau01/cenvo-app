using Cenvo.Modules.Schedule.Application.Templates;

namespace Cenvo.Modules.Schedule.Application.Planning;

/// <summary>The manager's weekly grid: shifts × days, who is on them, and what needs attention.</summary>
public class WeekQueryService(AppDbContext db, ICurrentUser user, IStaffDirectory staff, IEmployeeDirectory employees,
    TemplateService templates)
{
    public async Task<WeekDto> Week(DateOnly anyDay, CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(ScheduleErrors.NoBranch);
        var weekStart = Domain.Calculations.Week.StartOf(anyDay);
        var shifts = (await templates.List(ct)).Where(t => t.IsActive).ToList();
        var assignments = await db.Set<ShiftAssignment>().AsNoTracking()
            .Where(a => a.Date >= weekStart && a.Date < weekStart.AddDays(7))
            .OrderBy(a => a.EmployeeName)
            .Select(a => new AssignmentDto
            {
                Id = a.Id, Date = a.Date, ShiftTemplateId = a.ShiftTemplateId, EmployeeId = a.EmployeeId, EmployeeName = a.EmployeeName,
            })
            .ToListAsync(ct);

        var people = await staff.InBranch(branchId, role: null, ct);
        var peopleIds = people.Select(p => p.UserId).Union(assignments.Select(a => a.EmployeeId)).ToList();
        var departments = await employees.Departments(peopleIds, ct);
        var hours = await db.WeeklyHours(peopleIds, weekStart, ct);
        var limit = (await db.ScheduleSettingsFor(branchId, ct)).MaxWeeklyHours;

        var staffRows = people.Select(p =>
        {
            var h = hours.GetValueOrDefault(p.UserId);
            return new WeekStaffDto(p.UserId, p.FullName, departments.TryGetValue(p.UserId, out var d) ? d : null, h, OverLimit: h > limit);
        }).ToList();

        return new WeekDto(weekStart, limit, shifts, assignments, staffRows, Shortfalls(weekStart, shifts, assignments, departments));
    }

    private static List<ShortfallDto> Shortfalls(DateOnly weekStart, List<TemplateDto> shifts, List<AssignmentDto> assignments,
        Dictionary<Guid, Department> departments) =>
        (from day in Domain.Calculations.Week.Days(weekStart)
         from shift in shifts
         let onShift = assignments.Where(a => a.Date == day && a.ShiftTemplateId == shift.Id)
             .Select(a => departments.TryGetValue(a.EmployeeId, out var d) ? d : (Department?)null)
         from gap in Staffing.Shortfalls(shift.Needs.Select(n => new StaffingNeed(n.Department, n.MinStaff)), onShift)
         select new ShortfallDto(day, shift.Id, gap.Department, gap.Required, gap.Assigned)).ToList();
}
