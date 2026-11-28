namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>
/// Manager sends reminders for a selection of due customers. Each reason is claimed first (unique row, saved), then sent,
/// then marked sent / failed — so two devices never send the same reminder, and a crash mid-way leaves only claims
/// that expire after <see cref="CustomerReminder.PendingTimeout"/>.
/// </summary>
public class ReminderService(
    AppDbContext db, ICurrentUser user, IEnumerable<IReminderStrategy> strategies, IBranchDirectory branches,
    IReminderSender sender)
{
    public const int MaxPerSend = 100;

    public async Task SaveSettings(ReminderSettingsInput input, CancellationToken ct)
    {
        var settings = await db.ReminderSettingsFor(CurrentBranch(), ct);
        settings.Update(input.ReturnAfterDays, input.ExpiringWithinDays, input.LowUsesThreshold,
            input.ReturnTemplate, input.ExpiringTemplate, input.LowUsesTemplate);
        if (db.Entry(settings).State == EntityState.Detached) db.Add(settings);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetAcceptsReminders(Guid customerId, SetAcceptsRemindersInput input, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().FindAsync([customerId], ct) ?? throw new NotFoundException("khách hàng", customerId);
        customer.AcceptsReminders = input.Accepts;
        await db.SaveChangesAsync(ct);
    }

    public async Task<SendRemindersResult> Send(SendRemindersInput input, CancellationToken ct)
    {
        var refIds = (input.RefIds ?? []).Distinct().ToList();
        if (refIds.Count == 0) throw new BusinessException(CarWashErrors.ReminderNothingSelected);
        if (refIds.Count > MaxPerSend) throw new BusinessException(CarWashErrors.ReminderTooMany);

        var branchId = CurrentBranch();
        var strategy = strategies.Single(s => s.Kind == input.Kind);
        var settings = await db.ReminderSettingsFor(branchId, ct);
        var now = DateTime.UtcNow;

        // Re-check the selection: the list on screen may be stale (sent from another device, customer came back…).
        var due = await strategy.Due(branchId, settings, now, refIds).ToListAsync(ct);
        var claims = await Claim(input.Kind, due, strategy.Template(settings), await branches.Contact(branchId, ct), now, ct);

        var failures = new List<ReminderFailureDto>();
        try
        {
            foreach (var (row, reminder) in claims)
            {
                var result = await SendOne(reminder, ct);
                if (result.Succeeded) reminder.MarkSent(result.ProviderMessageId, DateTime.UtcNow);
                else
                {
                    reminder.MarkFailed(result.Error);
                    failures.Add(new ReminderFailureDto(row.RefId, row.CustomerName, reminder.LastError!));
                }
            }
        }
        finally
        {
            // Messages already out must be recorded even if the client has gone away; unsent claims stay Pending.
            await db.SaveChangesAsync(CancellationToken.None);
        }
        return new SendRemindersResult(Sent: claims.Count - failures.Count, Skipped: refIds.Count - claims.Count, Failures: failures);
    }

    private async Task<List<(DueReminderRow Row, CustomerReminder Reminder)>> Claim(ReminderKind kind, List<DueReminderRow> due,
        string template, BranchContact? branch, DateTime now, CancellationToken ct)
    {
        var dueIds = due.Select(r => r.RefId).ToList();
        var earlier = await db.Set<CustomerReminder>().Where(r => r.Kind == kind && dueIds.Contains(r.RefId))
            .ToDictionaryAsync(r => r.RefId, ct);

        var claims = new List<(DueReminderRow, CustomerReminder)>();
        foreach (var row in due)
        {
            var message = ReminderMessages.Render(template, row, branch, now);
            if (earlier.TryGetValue(row.RefId, out var reminder)) reminder.Retry(row.Phone, message, user.UserId, now);
            else db.Add(reminder = CustomerReminder.Claim(kind, row.RefId, row.CustomerId, row.Phone, message, user.UserId, now));
            claims.Add((row, reminder));
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            // Unique (kind, ref) taken: another device claimed the same reminder a moment ago.
            throw new BusinessException(CarWashErrors.ReminderSendingElsewhere);
        }
        return claims;
    }

    private async Task<ReminderSendResult> SendOne(CustomerReminder reminder, CancellationToken ct)
    {
        try
        {
            return await sender.Send(reminder.Phone, reminder.Message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ReminderSendResult.Failed(CarWashErrors.ReminderSendFailed);
        }
    }

    private Guid CurrentBranch() => user.BranchId ?? throw new BusinessException(CarWashErrors.NoBranch);
}
