namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>A package with uses left that expires within N days — "come and use it".</summary>
public class MembershipExpiringReminderStrategy(AppDbContext db) : MembershipReminderStrategy(db)
{
    public override ReminderKind Kind => ReminderKind.MembershipExpiring;

    public override string Template(ReminderSettings settings) => settings.ExpiringTemplate;

    protected override IQueryable<CustomerMembership> DueAmong(IQueryable<CustomerMembership> usable, ReminderSettings settings,
        DateTime now)
    {
        var expiresBefore = now.AddDays(settings.ExpiringWithinDays);
        return usable.Where(m => m.RemainingUses > 0 && m.ExpiresAt <= expiresBefore);
    }
}
