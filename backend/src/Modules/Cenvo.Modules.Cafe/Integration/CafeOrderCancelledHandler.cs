namespace Cenvo.Modules.Cafe.Integration;

/// <summary>Paid order cancelled → ingredients returned to stock (reversal entries).</summary>
public class CafeOrderCancelledHandler(ISaleStockIssuer stockIssuer) : IDomainEventHandler<CafeOrderCancelled>
{
    public Task Handle(CafeOrderCancelled e, CancellationToken ct) =>
        stockIssuer.CancelByRef(CafeOrder.StockRefType, e.OrderId, $"Hủy hóa đơn {e.Code}: {e.Reason}", ct);
}
