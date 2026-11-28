namespace Cenvo.Modules.CarWash.Integration;

/// <summary>A washed ticket is cancelled → supplies go back to stock (reversal entries).</summary>
public class WashTicketCancelledHandler(ISaleStockIssuer stockIssuer) : IDomainEventHandler<WashTicketCancelled>
{
    public Task Handle(WashTicketCancelled e, CancellationToken ct) =>
        e.WasCompleted
            ? stockIssuer.CancelByRef(WashTicket.StockRefType, e.TicketId, $"Hủy phiếu rửa xe {e.Code}: {e.Reason}", ct)
            : Task.CompletedTask;
}
