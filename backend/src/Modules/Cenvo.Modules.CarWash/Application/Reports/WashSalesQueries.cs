namespace Cenvo.Modules.CarWash.Application.Reports;

public static class WashSalesQueries
{
    /// <summary>Tickets paid in [fromUtc, toUtc) and not cancelled since (a cancelled paid ticket was refunded).</summary>
    public static IQueryable<WashTicket> PaidBetween(this IQueryable<WashTicket> tickets, DateTime fromUtc, DateTime toUtc) =>
        tickets.Where(ticket => ticket.PaidAt >= fromUtc && ticket.PaidAt < toUtc && ticket.Status != WashStatus.Cancelled);

    /// <summary>
    /// Packages sold in [fromUtc, toUtc) and not cancelled, by the current branch (all branches for a chain-wide admin).
    /// Memberships are chain-wide entities with no branch filter, so the selling branch is checked here.
    /// </summary>
    public static IQueryable<CustomerMembership> SoldBetween(this IQueryable<CustomerMembership> memberships, ICurrentUser user,
        DateTime fromUtc, DateTime toUtc) =>
        memberships.Where(membership => (user.IsAllBranches || membership.SoldBranchId == user.BranchId)
                                        && membership.PaidAt >= fromUtc && membership.PaidAt < toUtc
                                        && membership.Status != MembershipStatus.Cancelled);
}
