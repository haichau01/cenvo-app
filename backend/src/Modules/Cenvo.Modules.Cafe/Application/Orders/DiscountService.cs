namespace Cenvo.Modules.Cafe.Application.Orders;

/// <summary>
/// Order and item discounts. A cashier may discount up to <see cref="DiscountPolicy.CashierMaxPercent"/>% alone;
/// above it a manager approves on the same device (their own credentials), or applies it while signed in.
/// </summary>
public class DiscountService(
    AppDbContext db,
    OrderQueryService queries,
    IKitchenNotifier notifier,
    IManagerApproval managerApproval,
    ICurrentUser user)
{
    public async Task<OrderDto> DiscountOrder(Guid orderId, DiscountInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        var approval = await Approval(input.Approval, order.BranchId, ct);
        order.ApplyDiscount(input.Amount, input.Percent, input.Reason, user.UserId, user.FullName, approval);
        return await Save(order, ct);
    }

    public async Task<OrderDto> DiscountLine(Guid orderId, Guid lineId, LineDiscountInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        var approval = await Approval(input.Approval, order.BranchId, ct);
        order.DiscountLine(lineId, input.Amount, input.Reason, user.UserId, user.FullName, approval);
        return await Save(order, ct);
    }

    /// <summary>A signed-in manager approves their own discount; otherwise the typed manager account is checked.</summary>
    private async Task<DiscountApproval?> Approval(ManagerCredentials? credentials, Guid branchId, CancellationToken ct)
    {
        if (user.IsManager()) return new DiscountApproval(user.UserId!.Value, user.FullName);
        if (credentials == null) return null;
        var manager = await managerApproval.Approve(credentials.UserName, credentials.Password, branchId, ct);
        return new DiscountApproval(manager.UserId, manager.FullName);
    }

    private async Task<OrderDto> Save(CafeOrder order, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(order.BranchId, ct);
        return await queries.Get(order.Id, ct);
    }
}
