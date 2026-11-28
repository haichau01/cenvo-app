namespace Cenvo.Modules.CarWash.Application.Reports;

/// <summary>Car wash revenue, packages, supplies cost, commission and gross profit — every total is grouped in SQL.</summary>
public class WashSalesReportService(AppDbContext db, ISaleCostQuery costs, ICurrentUser user)
{
    public async Task<WashSalesSummary> Sales(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));
        var paid = db.Set<WashTicket>().AsNoTracking().PaidBetween(fromUtc, toUtc);
        var sold = db.Set<CustomerMembership>().AsNoTracking().SoldBetween(user, fromUtc, toUtc);

        var daily = MergeDays(await TicketDays(paid, ct), await MembershipDays(sold, ct));
        var tickets = daily.Sum(d => d.Tickets);
        var washRevenue = daily.Sum(d => d.WashRevenue);
        var coverage = await MembershipCoverage(paid, ct);

        return new WashSalesSummary(
            From: from,
            To: to,
            Tickets: tickets,
            Cancelled: await CancelledCount(fromUtc, toUtc, ct),
            WashRevenue: washRevenue,
            MembershipsSold: await sold.CountAsync(ct),
            MembershipRevenue: daily.Sum(d => d.MembershipRevenue),
            Revenue: daily.Sum(d => d.Revenue),
            MembershipUses: coverage.Uses,
            MembershipCovered: coverage.Covered,
            SupplyCost: daily.Sum(d => d.SupplyCost),
            Commission: daily.Sum(d => d.Commission),
            GrossProfit: daily.Sum(d => d.GrossProfit),
            AverageTicket: tickets == 0 ? 0 : Math.Round(washRevenue / tickets, 0),
            ByMethod: await RevenueByMethod(paid, sold, ct),
            Daily: daily,
            ByService: await ByService(paid, ct),
            ByVehicle: await ByVehicle(paid, ct));
    }

    private class DayRow
    {
        public required DateTime Day { get; init; }
        public required int Count { get; init; }
        public required decimal Revenue { get; init; }
        public decimal SupplyCost { get; init; }
        public decimal Commission { get; init; }
    }

    /// <summary>Per Vietnam day: tickets, money, and the supplies (posted sale issues) and commission of those tickets.</summary>
    private Task<List<DayRow>> TicketDays(IQueryable<WashTicket> paid, CancellationToken ct) =>
        (from ticket in paid
         join cost in costs.PostedCosts(WashTicket.StockRefType) on ticket.Id equals cost.RefId into ticketCosts
         from cost in ticketCosts.DefaultIfEmpty()
         let day = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(ticket.PaidAt!.Value, VnTime.ZoneId).Date
         group new { ticket.Total, Cost = (decimal?)cost.Cost ?? 0, Commission = ticket.Staff.Sum(s => s.Commission) } by day
         into byDay
         select new DayRow
         {
             Day = byDay.Key,
             Count = byDay.Count(),
             Revenue = byDay.Sum(t => t.Total),
             SupplyCost = byDay.Sum(t => t.Cost),
             Commission = byDay.Sum(t => t.Commission),
         })
        .ToListAsync(ct);

    private static Task<List<DayRow>> MembershipDays(IQueryable<CustomerMembership> sold, CancellationToken ct) =>
        sold.GroupBy(membership => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(membership.PaidAt, VnTime.ZoneId).Date)
            .Select(byDay => new DayRow { Day = byDay.Key, Count = byDay.Count(), Revenue = byDay.Sum(m => m.Price) })
            .ToListAsync(ct);

    /// <summary>One row per day that had tickets or package sales, oldest first.</summary>
    private static List<WashDailyRevenue> MergeDays(List<DayRow> ticketDays, List<DayRow> membershipDays)
    {
        var tickets = ticketDays.ToDictionary(d => DateOnly.FromDateTime(d.Day));
        var memberships = membershipDays.ToDictionary(d => DateOnly.FromDateTime(d.Day));
        return tickets.Keys.Union(memberships.Keys).Order()
            .Select(date =>
            {
                var wash = tickets.GetValueOrDefault(date);
                var membershipRevenue = memberships.GetValueOrDefault(date)?.Revenue ?? 0;
                var washRevenue = wash?.Revenue ?? 0;
                var supplyCost = wash?.SupplyCost ?? 0;
                var commission = wash?.Commission ?? 0;
                var revenue = washRevenue + membershipRevenue;
                return new WashDailyRevenue(Date: date, Tickets: wash?.Count ?? 0, WashRevenue: washRevenue,
                    MembershipRevenue: membershipRevenue, Revenue: revenue, SupplyCost: supplyCost, Commission: commission,
                    GrossProfit: revenue - supplyCost - commission);
            })
            .ToList();
    }

    private record Coverage(int Uses, decimal Covered);

    private static async Task<Coverage> MembershipCoverage(IQueryable<WashTicket> paid, CancellationToken ct) =>
        new(Uses: await paid.SelectMany(t => t.Lines).CountAsync(line => line.MembershipId != null, ct),
            Covered: await paid.SumAsync(t => t.MembershipCovered, ct));

    /// <summary>Counted by cancellation time: a cancelled ticket is often unpaid, so PaidAt is empty.</summary>
    private Task<int> CancelledCount(DateTime fromUtc, DateTime toUtc, CancellationToken ct) =>
        db.Set<WashTicket>().CountAsync(t => t.Status == WashStatus.Cancelled && t.CancelledAt >= fromUtc && t.CancelledAt < toUtc, ct);

    /// <summary>Tickets and package sales together; every method is present (0 when unused) for a fixed set of columns.</summary>
    private static async Task<Dictionary<PaymentMethod, decimal>> RevenueByMethod(IQueryable<WashTicket> paid,
        IQueryable<CustomerMembership> sold, CancellationToken ct)
    {
        var tickets = await paid.SelectMany(t => t.Payments).GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.Method, x => x.Amount, ct);
        var memberships = await sold.SelectMany(m => m.Payments).GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.Method, x => x.Amount, ct);
        return Enum.GetValues<PaymentMethod>().ToDictionary(method => method,
            method => tickets.GetValueOrDefault(method) + memberships.GetValueOrDefault(method));
    }

    private static Task<List<WashServiceSales>> ByService(IQueryable<WashTicket> paid, CancellationToken ct) =>
        paid.SelectMany(t => t.Lines)
            .GroupBy(line => new { line.OfferingId, line.Name })
            .OrderByDescending(byService => byService.Count()) // order before projecting: EF cannot sort on DTO members
            .Take(Limits.MaxListSize)
            .Select(byService => new WashServiceSales
            {
                OfferingId = byService.Key.OfferingId,
                Name = byService.Key.Name,
                Count = byService.Count(),
                MembershipUses = byService.Count(line => line.MembershipId != null),
                Revenue = byService.Sum(line => line.Amount),
            })
            .ToListAsync(ct);

    private static Task<List<WashVehicleSales>> ByVehicle(IQueryable<WashTicket> paid, CancellationToken ct) =>
        paid.GroupBy(t => t.VehicleType)
            .OrderByDescending(byType => byType.Count())
            .Select(byType => new WashVehicleSales
            {
                VehicleType = byType.Key, Tickets = byType.Count(), Revenue = byType.Sum(t => t.Total),
            })
            .ToListAsync(ct);
}
