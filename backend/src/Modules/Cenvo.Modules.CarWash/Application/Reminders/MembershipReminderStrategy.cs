namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>
/// Package reminders, sent by the branch that sold the package. The reason is the membership, so each package gets each
/// kind of reminder at most once. Subclasses only say which packages are due.
/// </summary>
public abstract class MembershipReminderStrategy(AppDbContext db) : IReminderStrategy
{
    public abstract ReminderKind Kind { get; }

    public abstract string Template(ReminderSettings settings);

    protected AppDbContext Db => db;

    /// <summary>Which of the branch's usable packages are due.</summary>
    protected abstract IQueryable<CustomerMembership> DueAmong(IQueryable<CustomerMembership> usable, ReminderSettings settings,
        DateTime now);

    public IQueryable<DueReminderRow> Due(Guid branchId, ReminderSettings settings, DateTime now, IReadOnlyCollection<Guid>? onlyRefIds)
    {
        var kind = Kind;
        var blocking = db.Set<CustomerReminder>().Blocking(kind, now);
        var earlier = db.Set<CustomerReminder>().For(kind);

        var memberships = DueAmong(db.Set<CustomerMembership>().AsNoTracking().ActiveSoldAt(branchId, now), settings, now)
            .Where(m => !blocking.Any(r => r.RefId == m.Id));
        if (onlyRefIds != null) memberships = memberships.Where(m => onlyRefIds.Contains(m.Id));

        return memberships
            .Join(db.Set<Customer>().Where(c => c.AcceptsReminders), m => m.CustomerId, c => c.Id,
                (membership, customer) => new { membership, customer })
            .OrderBy(x => x.membership.ExpiresAt)
            .Take(Limits.MaxListSize)
            .Select(x => new DueReminderRow
            {
                Kind = kind,
                RefId = x.membership.Id,
                CustomerId = x.customer.Id,
                CustomerName = x.customer.Name,
                Phone = x.customer.Phone,
                PackageName = x.membership.PackageName,
                RemainingUses = x.membership.RemainingUses,
                ExpiresAt = x.membership.ExpiresAt,
                LastError = earlier.Where(r => r.RefId == x.membership.Id).Select(r => r.LastError).FirstOrDefault(),
            });
    }
}
