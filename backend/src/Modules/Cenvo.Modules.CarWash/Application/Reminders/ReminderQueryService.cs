namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>Reminder settings, who is due now (with the exact message), and the send log of the current branch.</summary>
public class ReminderQueryService(
    AppDbContext db, ICurrentUser user, IEnumerable<IReminderStrategy> strategies, IBranchDirectory branches)
{
    public async Task<ReminderSettingsDto> Settings(CancellationToken ct)
    {
        var s = await db.ReminderSettingsFor(CurrentBranch(), ct);
        return new ReminderSettingsDto(
            ReturnAfterDays: s.ReturnAfterDays, ExpiringWithinDays: s.ExpiringWithinDays, LowUsesThreshold: s.LowUsesThreshold,
            ReturnTemplate: s.ReturnTemplate, ExpiringTemplate: s.ExpiringTemplate, LowUsesTemplate: s.LowUsesTemplate,
            Placeholders: ReminderTemplate.Placeholders, MaxTemplateLength: ReminderTemplate.MaxLength);
    }

    /// <summary>Every kind's due reminders, each capped at <see cref="Limits.MaxListSize"/>.</summary>
    public async Task<List<DueReminderDto>> Due(CancellationToken ct)
    {
        var branchId = CurrentBranch();
        var settings = await db.ReminderSettingsFor(branchId, ct);
        var branch = await branches.Contact(branchId, ct);
        var now = DateTime.UtcNow;

        var result = new List<DueReminderDto>();
        foreach (var strategy in strategies.OrderBy(s => s.Kind))
        {
            var rows = await strategy.Due(branchId, settings, now, onlyRefIds: null).ToListAsync(ct);
            var template = strategy.Template(settings);
            result.AddRange(rows.Select(row => new DueReminderDto(row, ReminderMessages.Render(template, row, branch, now))));
        }
        return result;
    }

    /// <summary>Reminders attempted in the period (Vietnam days), newest first.</summary>
    public Task<List<ReminderLogDto>> History(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var start = VnTime.StartOfDayUtc(from);
        var end = VnTime.StartOfDayUtc(to.AddDays(1));
        return db.Set<CustomerReminder>().AsNoTracking()
            .Where(r => r.AttemptedAt >= start && r.AttemptedAt < end)
            .OrderByDescending(r => r.AttemptedAt)
            .Take(Limits.MaxListSize)
            .Join(db.Set<Customer>(), r => r.CustomerId, c => c.Id, (r, c) => new ReminderLogDto
            {
                Id = r.Id,
                Kind = r.Kind,
                CustomerId = r.CustomerId,
                CustomerName = c.Name,
                Phone = r.Phone,
                Message = r.Message,
                Status = r.Status,
                Attempts = r.Attempts,
                AttemptedAt = r.AttemptedAt,
                SentAt = r.SentAt,
                LastError = r.LastError,
            })
            .ToListAsync(ct);
    }

    private Guid CurrentBranch() => user.BranchId ?? throw new BusinessException(CarWashErrors.NoBranch);
}
