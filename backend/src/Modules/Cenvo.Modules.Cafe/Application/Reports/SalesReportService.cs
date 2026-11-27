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
        var paidInRange = db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.PaidAt >= fromUtc && o.PaidAt < toUtc);

        var orders = await paidInRange.Select(o => new
        {
            o.Id, PaidAt = o.PaidAt!.Value, o.Total, Discount = o.DiscountAmount + o.Lines.Sum(l => l.DiscountAmount),
        }).ToListAsync(ct);
        var cancelled = await db.Set<CafeOrder>()
            .CountAsync(o => o.Status == OrderStatus.Cancelled && o.PaidAt >= fromUtc && o.PaidAt < toUtc, ct);
        var byMethod = await paidInRange.SelectMany(o => o.Payments).GroupBy(p => p.Method)
            .Select(g => new { g.Key, Amount = g.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Amount, ct);
        var topItems = await paidInRange.SelectMany(o => o.Lines).GroupBy(l => new { l.ProductId, l.Name })
            .Select(g => new TopItem(g.Key.ProductId, g.Key.Name, g.Sum(l => l.Quantity), g.Sum(l => l.Amount)))
            .OrderByDescending(x => x.Quantity).Take(TopItemCount).ToListAsync(ct);
        var costByOrder = await CostByOrder(orders.Select(o => o.Id).ToList(), ct);

        var daily = orders.GroupBy(o => DateOnly.FromDateTime(VnTime.ToLocal(o.PaidAt)))
            .Select(g =>
            {
                var revenue = g.Sum(o => o.Total);
                var cost = g.Sum(o => costByOrder.GetValueOrDefault(o.Id));
                return new DailyRevenue(g.Key, g.Count(), revenue, g.Sum(o => o.Discount), cost, revenue - cost);
            })
            .OrderBy(d => d.Date).ToList();

        var totalRevenue = orders.Sum(o => o.Total);
        var totalCost = daily.Sum(d => d.Cost);
        return new SalesSummary(from, to, orders.Count, cancelled, totalRevenue, daily.Sum(d => d.Discount), totalCost,
            totalRevenue - totalCost, orders.Count == 0 ? 0 : Math.Round(totalRevenue / orders.Count, 0),
            Enum.GetValues<PaymentMethod>().ToDictionary(m => m, m => byMethod.GetValueOrDefault(m)), daily, topItems);
    }

    /// <summary>Cost of goods = total value of each order's still-valid sale issue documents.</summary>
    private Task<Dictionary<Guid, decimal>> CostByOrder(List<Guid> orderIds, CancellationToken ct) =>
        db.Set<StockDocument>().AsNoTracking()
            .Where(d => d.RefType == CafeOrder.StockRefType && d.RefId != null && orderIds.Contains(d.RefId.Value)
                        && d.Status == DocumentStatus.Posted)
            .GroupBy(d => d.RefId!.Value)
            .Select(g => new { OrderId = g.Key, Cost = g.SelectMany(d => d.Lines).Sum(l => l.Amount) })
            .ToDictionaryAsync(x => x.OrderId, x => x.Cost, ct);
}
