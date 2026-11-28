namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>
/// Customers whose latest wash in the chain was at this branch, at least N days ago. The reason is that latest ticket,
/// so each visit is reminded at most once and a new visit starts over.
/// </summary>
public class ReturnVisitReminderStrategy(AppDbContext db) : IReminderStrategy
{
    private const ReminderKind ThisKind = ReminderKind.ReturnVisit;

    public ReminderKind Kind => ThisKind;

    public string Template(ReminderSettings settings) => settings.ReturnTemplate;

    public IQueryable<DueReminderRow> Due(Guid branchId, ReminderSettings settings, DateTime now, IReadOnlyCollection<Guid>? onlyRefIds)
    {
        var washedBefore = now.AddDays(-settings.ReturnAfterDays);
        var blocking = db.Set<CustomerReminder>().Blocking(ThisKind, now);
        var earlier = db.Set<CustomerReminder>().For(ThisKind);
        var tickets = db.Set<WashTicket>();

        var customers = db.Set<Customer>().AsNoTracking()
            .Where(c => c.AcceptsReminders && c.LastTicketId != null
                        && c.LastWashBranchId == branchId && c.LastWashAt <= washedBefore)
            .Where(c => !blocking.Any(r => r.RefId == c.LastTicketId));
        if (onlyRefIds != null) customers = customers.Where(c => onlyRefIds.Contains(c.LastTicketId!.Value));

        return customers
            .OrderBy(c => c.LastWashAt)
            .Take(Limits.MaxListSize)
            .Select(c => new DueReminderRow
            {
                Kind = ThisKind,
                RefId = c.LastTicketId!.Value,
                CustomerId = c.Id,
                CustomerName = c.Name,
                Phone = c.Phone,
                Plate = tickets.Where(t => t.Id == c.LastTicketId).Select(t => t.Plate).FirstOrDefault(),
                LastWashAt = c.LastWashAt,
                LastError = earlier.Where(r => r.RefId == c.LastTicketId).Select(r => r.LastError).FirstOrDefault(),
            });
    }
}
