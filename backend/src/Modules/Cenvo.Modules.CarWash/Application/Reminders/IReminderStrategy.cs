namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>One kind of reminder: who is due and which template it uses. A new kind = a new strategy class.</summary>
public interface IReminderStrategy
{
    ReminderKind Kind { get; }

    string Template(ReminderSettings settings);

    /// <summary>Reasons due now at the branch, oldest first; already sent / being sent and opted-out customers excluded.</summary>
    /// <param name="onlyRefIds">Restrict to these reasons (re-checking a selection before sending); null = all.</param>
    IQueryable<DueReminderRow> Due(Guid branchId, ReminderSettings settings, DateTime now, IReadOnlyCollection<Guid>? onlyRefIds);
}

internal static class ReminderQueries
{
    /// <summary>Reminders that block sending the same reason again: sent, or a send still in progress.</summary>
    public static IQueryable<CustomerReminder> Blocking(this IQueryable<CustomerReminder> reminders, ReminderKind kind, DateTime now)
    {
        var stuckBefore = now - CustomerReminder.PendingTimeout;
        return reminders.Where(r => r.Kind == kind
                                    && (r.Status == ReminderStatus.Sent
                                        || (r.Status == ReminderStatus.Pending && r.AttemptedAt >= stuckBefore)));
    }

    public static IQueryable<CustomerReminder> For(this IQueryable<CustomerReminder> reminders, ReminderKind kind) =>
        reminders.Where(r => r.Kind == kind);

    /// <summary>Usable package with uses left at the selling branch.</summary>
    public static IQueryable<CustomerMembership> ActiveSoldAt(this IQueryable<CustomerMembership> memberships, Guid branchId,
        DateTime now) =>
        memberships.Where(m => m.SoldBranchId == branchId && m.Status == MembershipStatus.Active && m.ExpiresAt > now);
}
