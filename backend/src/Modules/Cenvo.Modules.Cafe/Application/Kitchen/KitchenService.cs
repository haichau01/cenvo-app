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
        var since = DateTime.UtcNow - Lookback;
        var rows = await db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.CreatedAt >= since && o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Lines.Where(l => InQueue.Contains(l.KitchenStatus)).Select(l => new
            {
                o.Id, o.Code, o.TableId, o.Type, LineId = l.Id, l.Name, l.Quantity, l.Note, l.KitchenStatus, l.SentAt,
                Toppings = l.Toppings.Select(t => new { t.Name, t.Quantity }).ToList(),
            }))
            .OrderBy(x => x.SentAt).ToListAsync(ct);

        var tableIds = rows.Where(r => r.TableId != null).Select(r => r.TableId!.Value).Distinct().ToList();
        var tableNames = await db.Set<DiningTable>().Where(t => tableIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return rows.Select(r => new KitchenTicket(r.Id, r.Code, r.TableId == null ? null : tableNames.GetValueOrDefault(r.TableId.Value),
            r.Type, r.LineId, r.Name, r.Quantity, r.Note,
            r.Toppings.Select(t => t.Quantity == 1 ? t.Name : $"{t.Name} x{t.Quantity:0.##}").ToList(),
            r.KitchenStatus, r.SentAt)).ToList();
    }

    public async Task SetStatus(Guid lineId, KitchenStatus status, CancellationToken ct)
    {
        if (status == KitchenStatus.New) throw new BusinessException(CafeErrors.InvalidKitchenStatus);
        // Go through CafeOrder so the global filter blocks items from other branches.
        var order = await db.Set<CafeOrder>().Include(o => o.Lines)
                        .FirstOrDefaultAsync(o => o.Lines.Any(l => l.Id == lineId), ct)
                    ?? throw new NotFoundException("dòng món", lineId);
        order.Lines.Single(l => l.Id == lineId).KitchenStatus = status;
        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(order.BranchId, ct);
    }
}
