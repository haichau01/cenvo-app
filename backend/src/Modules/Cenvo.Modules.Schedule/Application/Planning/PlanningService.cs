using Cenvo.Modules.Schedule.Application.Templates;

namespace Cenvo.Modules.Schedule.Application.Planning;

/// <summary>Managers put people on shifts. Overlapping shifts (any branch) are refused; a long week only warns.</summary>
public class PlanningService(AppDbContext db, ICurrentUser user, IStaffDirectory staff, IBranchDirectory branches)
{
    public async Task<AssignResult> Assign(AssignInput input, CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(ScheduleErrors.NoBranch);
        var template = await db.Set<ShiftTemplate>().FirstOrDefaultAsync(t => t.Id == input.ShiftTemplateId, ct)
                       ?? throw new NotFoundException("ca làm", input.ShiftTemplateId);
        var employee = (await staff.InBranch(branchId, role: null, ct)).FirstOrDefault(s => s.UserId == input.EmployeeId)
                       ?? throw new BusinessException(ScheduleErrors.StaffNotInBranch);

        var assignment = ShiftAssignment.Create(template, employee, input.Date, VnTime.Today);
        await EnsureNoOverlap(assignment, ct);
        db.Add(assignment);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            throw new BusinessException(ScheduleErrors.AlreadyAssigned); // unique (employee, day, shift)
        }

        var hours = (await db.WeeklyHours([employee.UserId], Week.StartOf(input.Date), ct)).GetValueOrDefault(employee.UserId);
        var limit = (await db.ScheduleSettingsFor(branchId, ct)).MaxWeeklyHours;
        return new AssignResult(assignment.Id, hours, OverLimit: hours > limit);
    }

    public async Task Remove(Guid id, CancellationToken ct)
    {
        var assignment = await db.Set<ShiftAssignment>().FirstOrDefaultAsync(a => a.Id == id, ct)
                         ?? throw new NotFoundException("ca đã xếp", id);
        ShiftAssignment.EnsureNotPast(assignment.Date, VnTime.Today);
        db.Remove(assignment);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureNoOverlap(ShiftAssignment candidate, CancellationToken ct)
    {
        if (await db.Set<ShiftAssignment>().AnyAsync(a => a.EmployeeId == candidate.EmployeeId && a.Date == candidate.Date
                                                          && a.ShiftTemplateId == candidate.ShiftTemplateId, ct))
            throw new BusinessException(ScheduleErrors.AlreadyAssigned);

        var clash = await db.OfEmployeesInAllBranches([candidate.EmployeeId])
            .Between(candidate.StartsAt, candidate.EndsAt)
            .Select(a => new { a.ShiftName, a.Date, a.BranchId })
            .FirstOrDefaultAsync(ct);
        if (clash == null) return;

        var branch = await branches.Contact(clash.BranchId, ct);
        throw new BusinessException(ScheduleErrors.Overlap(candidate.EmployeeName, clash.ShiftName, clash.Date, branch?.Name ?? "chi nhánh khác"));
    }
}
