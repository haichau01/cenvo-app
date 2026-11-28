using Cenvo.Modules.Cafe.Application.Orders;

namespace Cenvo.Modules.Cafe.Application.Kitchen;

public record KitchenTicket(Guid OrderId, string OrderCode, string? TableName, OrderType Type, Guid LineId, string Name,
    decimal Quantity, string? Note, List<string> Toppings, KitchenStatus Status, DateTime? SentAt);

public record KitchenStatusInput(KitchenStatus Status);

/// <summary>Kitchen display: send items, read the queue, change item status.</summary>
public class KitchenService(AppDbContext db, OrderQueryService queries, IKitchenNotifier notifier)
{
    /// <summary>Only items sent within this window are shown — older ones are considered handled.</summary>
    private static readonly TimeSpan Lookback = TimeSpan.FromHours(12);

    private static readonly KitchenStatus[] InQueue = [KitchenStatus.Sent, KitchenStatus.Preparing, KitchenStatus.Done];

    public async Task<OrderDto> Send(Guid orderId, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        order.SendToKitchen(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(order.BranchId, ct);
        return await queries.Get(order.Id, ct);
    }

    public async Task<List<KitchenTicket>> Queue(CancellationToken ct)
    {
        var lines = await QueuedLines(ct);
        var tableNames = await TableNames(lines.Select(l => l.TableId), ct);
        return lines.Select(line => line.ToTicket(TableNameOf(line.TableId, tableNames))).ToList();
    }

    public async Task SetStatus(Guid lineId, KitchenStatus status, CancellationToken ct)
    {
        // Load through CafeOrder so the branch filter blocks items of other branches.
        var order = await db.Set<CafeOrder>().Include(o => o.Lines)
                        .FirstOrDefaultAsync(o => o.Lines.Any(l => l.Id == lineId), ct)
                    ?? throw new NotFoundException("dòng món", lineId);
        order.ChangeKitchenStatus(lineId, status);
        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(order.BranchId, ct);
    }

    /// <summary>Items of recent, non-cancelled orders that the kitchen still has to handle, oldest first.</summary>
    private Task<List<QueuedLine>> QueuedLines(CancellationToken ct)
    {
        var since = DateTime.UtcNow - Lookback;
        return db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.CreatedAt >= since && o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Lines
                .Where(l => InQueue.Contains(l.KitchenStatus))
                .Select(l => new QueuedLine
                {
                    OrderId = o.Id, OrderCode = o.Code, TableId = o.TableId, Type = o.Type,
                    LineId = l.Id, Name = l.Name, Quantity = l.Quantity, Note = l.Note,
                    Status = l.KitchenStatus, SentAt = l.SentAt,
                    Toppings = l.Toppings.Select(t => new QueuedTopping(t.Name, t.Quantity)).ToList(),
                }))
            .OrderBy(l => l.SentAt)
            .ToListAsync(ct);
    }

    /// <summary>Separate lookup by id — a join here would need IgnoreQueryFilters, which would drop the branch filter.</summary>
    private Task<Dictionary<Guid, string>> TableNames(IEnumerable<Guid?> tableIds, CancellationToken ct)
    {
        var ids = tableIds.OfType<Guid>().Distinct().ToList();
        return db.Set<DiningTable>().Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);
    }

    private static string? TableNameOf(Guid? tableId, Dictionary<Guid, string> names) =>
        tableId is { } id ? names.GetValueOrDefault(id) : null;

    private record QueuedTopping(string Name, decimal Quantity)
    {
        /// <summary>"Trân châu" for one portion, "Trân châu x2" otherwise.</summary>
        public string Label => Quantity == 1 ? Name : $"{Name} x{Quantity:0.##}";
    }

    private class QueuedLine
    {
        public Guid OrderId { get; init; }
        public required string OrderCode { get; init; }
        public Guid? TableId { get; init; }
        public OrderType Type { get; init; }
        public Guid LineId { get; init; }
        public required string Name { get; init; }
        public decimal Quantity { get; init; }
        public string? Note { get; init; }
        public KitchenStatus Status { get; init; }
        public DateTime? SentAt { get; init; }
        public required List<QueuedTopping> Toppings { get; init; }

        public KitchenTicket ToTicket(string? tableName) => new(
            OrderId: OrderId, OrderCode: OrderCode, TableName: tableName, Type: Type, LineId: LineId, Name: Name,
            Quantity: Quantity, Note: Note, Toppings: Toppings.Select(t => t.Label).ToList(), Status: Status, SentAt: SentAt);
    }
}
