namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>
/// A package down to its last uses (or used up) — "renew it". Skipped when the customer already holds another package
/// for the same service with more uses left (they have renewed).
/// </summary>
public class MembershipLowUsesReminderStrategy(AppDbContext db) : MembershipReminderStrategy(db)
{
    public override ReminderKind Kind => ReminderKind.MembershipLowUses;

    public override string Template(ReminderSettings settings) => settings.LowUsesTemplate;

    protected override IQueryable<CustomerMembership> DueAmong(IQueryable<CustomerMembership> usable, ReminderSettings settings,
        DateTime now)
    {
        var threshold = settings.LowUsesThreshold;
        var renewals = Db.Set<CustomerMembership>()
            .Where(o => o.Status == MembershipStatus.Active && o.ExpiresAt > now && o.RemainingUses > threshold);
        return usable.Where(m => m.RemainingUses <= threshold
                                 && !renewals.Any(o => o.CustomerId == m.CustomerId && o.OfferingId == m.OfferingId && o.Id != m.Id));
    }
}
