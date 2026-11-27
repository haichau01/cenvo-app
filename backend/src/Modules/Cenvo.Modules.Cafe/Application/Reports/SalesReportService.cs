using Cenvo.Modules.Inventory.Domain;

namespace Cenvo.Modules.Cafe.Application.Reports;

public record DailyRevenue(DateOnly Date, int Orders, decimal Revenue, decimal Discount, decimal Cost, decimal GrossProfit);
public record TopItem(Guid ProductId, string Name, decimal Quantity, decimal Revenue);
public record SalesSummary(DateOnly From, DateOnly To, int Orders, int Cancelled, decimal Revenue, decimal Discount,
    decimal Cost, decimal GrossProfit, decimal AverageTicket, Dictionary<PaymentMethod, decimal> ByMethod,
    List<DailyRevenue> Daily, List<TopItem> TopItems);

/// <summary>Revenue, cost of goods (from sale issue documents), gross profit, best sellers.</summary>
public class SalesReportService(AppDbContext db)
{
    private const int TopItemCount = 10;

    public async Task<SalesSummary> Sales(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));
        var paid = db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.PaidAt >= fromUtc && o.PaidAt < toUtc);

        var orders = await PaidOrders(paid, ct);
        var costByOrder = await CostByOrder(orders.Select(o => o.Id).ToList(), ct);
        var daily = DailyTotals(orders, costByOrder);
        var revenue = orders.Sum(o => o.Total);
        var cost = daily.Sum(d => d.Cost);

        return new SalesSummary(
            From: from,
            To: to,
            Orders: orders.Count,
            Cancelled: await CancelledCount(fromUtc, toUtc, ct),
            Revenue: revenue,
            Discount: daily.Sum(d => d.Discount),
            Cost: cost,
            GrossProfit: revenue - cost,
            AverageTicket: orders.Count == 0 ? 0 : Math.Round(revenue / orders.Count, 0),
            ByMethod: await RevenueByMethod(paid, ct),
            Daily: daily,
            TopItems: await TopItems(paid, ct));
    }

    private record PaidOrder(Guid Id, DateTime PaidAt, decimal Total, decimal Discount);

    private static Task<List<PaidOrder>> PaidOrders(IQueryable<CafeOrder> paid, CancellationToken ct) =>
        paid.Select(o => new PaidOrder(
                o.Id,
                o.PaidAt!.Value,
                o.Total,
                o.DiscountAmount + o.Lines.Sum(l => l.DiscountAmount))) // order discount + line discounts
            .ToListAsync(ct);

    /// <summary>Counted by cancellation time: most cancelled orders were never paid, so PaidAt is empty.</summary>
    private Task<int> CancelledCount(DateTime fromUtc, DateTime toUtc, CancellationToken ct) =>
        db.Set<CafeOrder>().CountAsync(o => o.Status == OrderStatus.Cancelled && o.CancelledAt >= fromUtc && o.CancelledAt < toUtc, ct);

    /// <summary>Every payment method is present (0 when unused) so the client can render a fixed set of columns.</summary>
    private static async Task<Dictionary<PaymentMethod, decimal>> RevenueByMethod(IQueryable<CafeOrder> paid, CancellationToken ct)
    {
        var amounts = await paid.SelectMany(o => o.Payments)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.Method, x => x.Amount, ct);
        return Enum.GetValues<PaymentMethod>().ToDictionary(method => method, method => amounts.GetValueOrDefault(method));
    }

    private static Task<List<TopItem>> TopItems(IQueryable<CafeOrder> paid, CancellationToken ct) =>
        paid.SelectMany(o => o.Lines)
            .GroupBy(l => new { l.ProductId, l.Name })
            .OrderByDescending(g => g.Sum(l => l.Quantity)) // order before projecting: EF cannot sort on DTO members
            .Take(TopItemCount)
            .Select(g => new TopItem(g.Key.ProductId, g.Key.Name, g.Sum(l => l.Quantity), g.Sum(l => l.Amount)))
            .ToListAsync(ct);

    private static List<DailyRevenue> DailyTotals(List<PaidOrder> orders, Dictionary<Guid, decimal> costByOrder) =>
        orders.GroupBy(o => DateOnly.FromDateTime(VnTime.ToLocal(o.PaidAt)))
            .Select(day =>
            {
                var revenue = day.Sum(o => o.Total);
                var cost = day.Sum(o => costByOrder.GetValueOrDefault(o.Id));
                return new DailyRevenue(Date: day.Key, Orders: day.Count(), Revenue: revenue,
                    Discount: day.Sum(o => o.Discount), Cost: cost, GrossProfit: revenue - cost);
            })
            .OrderBy(d => d.Date)
            .ToList();

    /// <summary>Cost of goods = total value of each order's still-valid sale issue documents.</summary>
    private Task<Dictionary<Guid, decimal>> CostByOrder(List<Guid> orderIds, CancellationToken ct) =>
        db.Set<StockDocument>().AsNoTracking()
            .Where(d => d.RefType == CafeOrder.StockRefType && d.RefId != null && orderIds.Contains(d.RefId.Value)
                        && d.Status == DocumentStatus.Posted)
            .GroupBy(d => d.RefId!.Value)
            .Select(g => new { OrderId = g.Key, Cost = g.SelectMany(d => d.Lines).Sum(l => l.Amount) })
            .ToDictionaryAsync(x => x.OrderId, x => x.Cost, ct);
}
