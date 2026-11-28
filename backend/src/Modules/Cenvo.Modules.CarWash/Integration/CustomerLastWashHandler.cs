namespace Cenvo.Modules.CarWash.Integration;

/// <summary>
/// Keeps <see cref="Customer.LastWashAt"/> current (same transaction as the ticket): the return reminder counts from the
/// latest wash anywhere in the chain, so a customer who just washed at another branch is not reminded.
/// </summary>
public class CustomerLastWashHandler(AppDbContext db)
    : IDomainEventHandler<WashTicketCompleted>, IDomainEventHandler<WashTicketCancelled>
{
    public async Task Handle(WashTicketCompleted e, CancellationToken ct)
    {
        if (e.CustomerId == null) return;
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == e.CustomerId, ct);
        customer?.RecordWash(e.TicketId, e.BranchId, e.CompletedAt);
    }

    /// <summary>The cancelled ticket was the latest visit → fall back to the customer's previous finished wash.</summary>
    public async Task Handle(WashTicketCancelled e, CancellationToken ct)
    {
        if (!e.WasCompleted || e.CustomerId == null) return;
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == e.CustomerId && c.LastTicketId == e.TicketId, ct);
        if (customer == null) return;

        // IgnoreQueryFilters: the latest visit is chain-wide. Single table, filtered by this customer, and only the
        // ticket id / branch id / time are read — nothing of another branch is shown to the user.
        var previous = await db.Set<WashTicket>().IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.CustomerId == e.CustomerId && t.Id != e.TicketId && t.CompletedAt != null
                        && t.Status != WashStatus.Cancelled)
            .OrderByDescending(t => t.CompletedAt)
            .Select(t => new { t.Id, t.BranchId, t.CompletedAt })
            .FirstOrDefaultAsync(ct);
        customer.SetLastWash(previous?.Id, previous?.BranchId, previous?.CompletedAt);
    }
}
