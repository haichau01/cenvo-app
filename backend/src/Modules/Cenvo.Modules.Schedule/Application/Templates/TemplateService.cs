namespace Cenvo.Modules.Schedule.Application.Templates;

/// <summary>Managers: the shifts their branch runs, and the weekly hours limit.</summary>
public class TemplateService(AppDbContext db, ICurrentUser user)
{
    public async Task<List<TemplateDto>> List(CancellationToken ct) =>
        (await db.Set<ShiftTemplate>().AsNoTracking().Include(t => t.Requirements)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.StartTime)
            .Take(Limits.MaxListSize)
            .ToListAsync(ct))
        .Select(ToDto).ToList();

    public async Task<Guid> Save(Guid? id, TemplateInput input, CancellationToken ct)
    {
        RequireBranch();
        var template = id == null
            ? db.Add(new ShiftTemplate()).Entity
            : await db.Set<ShiftTemplate>().Include(t => t.Requirements).FirstOrDefaultAsync(t => t.Id == id, ct)
              ?? throw new NotFoundException("ca làm", id);
        template.Update(input.Name, input.StartTime, input.EndTime, input.SortOrder, input.IsActive,
            input.Needs.Select(n => new StaffingNeed(n.Department, n.MinStaff)).ToList());
        await db.SaveChangesAsync(ct);
        return template.Id;
    }

    /// <summary>Soft delete: past schedules keep their copy of the shift.</summary>
    public async Task Delete(Guid id, CancellationToken ct)
    {
        var template = await db.Set<ShiftTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("ca làm", id);
        template.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ScheduleSettingsDto> Settings(CancellationToken ct) =>
        new((await db.ScheduleSettingsFor(RequireBranch(), ct)).MaxWeeklyHours);

    public async Task SaveSettings(ScheduleSettingsDto input, CancellationToken ct)
    {
        var settings = await db.ScheduleSettingsFor(RequireBranch(), ct);
        settings.Update(input.MaxWeeklyHours);
        if (db.Entry(settings).State == EntityState.Detached) db.Add(settings);
        await db.SaveChangesAsync(ct);
    }

    public static TemplateDto ToDto(ShiftTemplate t) =>
        new(t.Id, t.Name, t.StartTime, t.EndTime, Overnight: t.EndTime <= t.StartTime, t.SortOrder, t.IsActive,
            t.Requirements.OrderBy(r => r.Department).Select(r => new StaffingNeedDto(r.Department, r.MinStaff)).ToList());

    private Guid RequireBranch() => user.BranchId ?? throw new BusinessException(ScheduleErrors.NoBranch);
}

internal static class ScheduleSettingsLoading
{
    /// <summary>The branch's settings, or the defaults while none were saved.</summary>
    public static async Task<ScheduleSettings> ScheduleSettingsFor(this AppDbContext db, Guid branchId, CancellationToken ct) =>
        await db.Set<ScheduleSettings>().FirstOrDefaultAsync(s => s.BranchId == branchId, ct) ?? new ScheduleSettings { BranchId = branchId };
}
