using Cenvo.Modules.Organization.Contracts;

namespace Cenvo.Modules.Cafe.Application.Orders;

/// <summary>What a printed receipt needs: the order plus the shop header and who took the payment.</summary>
public record ReceiptDto(OrderDto Order, BranchContact Store, string? CashierName);

public class ReceiptQueryService(AppDbContext db, OrderQueryService orders, IBranchDirectory branches)
{
    /// <summary>
    /// Receipt of an order of the current branch (the order lookup is branch-filtered).
    /// An open order prints as a pre-bill ("tạm tính"); a paid one as the final receipt.
    /// </summary>
    public async Task<ReceiptDto> Get(Guid orderId, CancellationToken ct)
    {
        var order = await orders.Get(orderId, ct);
        var source = await db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.BranchId, o.ShiftId })
            .FirstAsync(ct);

        var store = await branches.Contact(source.BranchId, ct) ?? new BranchContact(Name: "", Address: null, Phone: null);
        var cashier = source.ShiftId is { } shiftId
            ? await db.Set<Shift>().Where(s => s.Id == shiftId).Select(s => s.CashierName).FirstOrDefaultAsync(ct)
            : null;
        return new ReceiptDto(Order: order, Store: store, CashierName: cashier);
    }
}
