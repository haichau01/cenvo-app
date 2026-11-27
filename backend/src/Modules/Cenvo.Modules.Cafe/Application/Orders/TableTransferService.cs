namespace Cenvo.Modules.Cafe.Application.Orders;

/// <summary>Move, merge and split tables.</summary>
public class TableTransferService(
    AppDbContext db,
    OrderQueryService queries,
    IDocumentNumberGenerator numbers,
    IKitchenNotifier notifier)
{
    public async Task<OrderDto> Move(Guid orderId, MoveLinesInput input, CancellationToken ct)
    {
        var source = await db.LoadOrder(orderId, ct);
        var target = await ResolveTarget(source, input, ct);

        if (target == null) source.MoveToTable(input.ToTableId!.Value); // whole order to an empty table
        else source.MoveLinesTo(target, input.LineIds, DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(source.BranchId, ct);
        return await queries.Get((target ?? source).Id, ct);
    }

    /// <summary>Order receiving the lines; null = only the source order's table changes.</summary>
    private async Task<CafeOrder?> ResolveTarget(CafeOrder source, MoveLinesInput input, CancellationToken ct)
    {
        if (input.ToOrderId != null) return await db.LoadOrder(input.ToOrderId.Value, ct);

        if (input.ToTableId is { } tableId)
        {
            await db.EnsureTableExists(tableId, ct);
            var occupied = await db.FindOpenOrderAtTable(tableId, ct);
            if (occupied != null) return occupied;                       // merge into an occupied table
            if (source.IncludesAllLines(input.LineIds)) return null;     // move the whole table
        }
        return await OpenNewOrder(source.BranchId, input.ToTableId, ct); // split to an empty table / take-away
    }

    private async Task<CafeOrder> OpenNewOrder(Guid branchId, Guid? tableId, CancellationToken ct)
    {
        var code = await numbers.Next(branchId, CafeOrder.CodePrefix, DateTime.UtcNow, ct);
        var order = CafeOrder.Open(branchId, code, tableId == null ? OrderType.TakeAway : OrderType.DineIn, tableId, null);
        db.Add(order);
        return order;
    }
}
