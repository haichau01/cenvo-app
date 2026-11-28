namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <summary>Moves a vehicle along the queue. Completing it deducts supplies and records commission (domain event).</summary>
public class TicketFlowService(AppDbContext db, TicketQueryService queries, IWashNotifier notifier)
{
    public Task<TicketDto> Start(Guid id, CancellationToken ct) => Change(id, t => t.Start(DateTime.UtcNow), ct);

    public Task<TicketDto> ReturnToWaiting(Guid id, CancellationToken ct) => Change(id, t => t.ReturnToWaiting(), ct);

    public Task<TicketDto> Complete(Guid id, CancellationToken ct) => Change(id, t => t.Complete(DateTime.UtcNow), ct);

    public Task<TicketDto> Deliver(Guid id, CancellationToken ct) => Change(id, t => t.Deliver(DateTime.UtcNow), ct);

    private async Task<TicketDto> Change(Guid id, Action<WashTicket> change, CancellationToken ct)
    {
        var ticket = await db.LoadTicket(id, ct);
        change(ticket);
        await db.SaveChangesAsync(ct);
        await notifier.TicketsChanged(ticket.BranchId, ct);
        return await queries.Get(id, ct);
    }
}
