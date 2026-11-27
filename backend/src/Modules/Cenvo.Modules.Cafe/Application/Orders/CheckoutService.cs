using Cenvo.Modules.Cafe.Application.Shifts;

namespace Cenvo.Modules.Cafe.Application.Orders;

/// <summary>Paying and cancelling orders — both affect inventory (via domain events).</summary>
public class CheckoutService(
    AppDbContext db,
    ShiftService shifts,
    OrderQueryService queries,
    IKitchenNotifier notifier,
    ICurrentUser user)
{
    public async Task<OrderDto> Pay(Guid orderId, PayInput input, CancellationToken ct)
    {
        var shift = await shifts.CurrentShift(ct) ?? throw new BusinessException(CafeErrors.ShiftNotOpen);
        var order = await db.LoadOrder(orderId, ct);
        order.Pay(shift.Id, user.UserId, input.Payments, DateTime.UtcNow);
        return await Save(order, ct);
    }

    public async Task<OrderDto> Cancel(Guid orderId, CancelInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        order.Cancel(input.Reason, byManager: user.IsManager(), DateTime.UtcNow);
        return await Save(order, ct);
    }

    private async Task<OrderDto> Save(CafeOrder order, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(order.BranchId, ct);
        return await queries.Get(order.Id, ct);
    }
}
