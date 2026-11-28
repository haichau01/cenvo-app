namespace Cenvo.Modules.CarWash.Integration;

/// <summary>One vehicle washed → inventory deducts supplies per service (same transaction as the ticket).</summary>
public class WashTicketCompletedHandler(AppDbContext db, IUnitConverterFactory converters, ISaleStockIssuer stockIssuer)
    : IDomainEventHandler<WashTicketCompleted>
{
    public async Task Handle(WashTicketCompleted e, CancellationToken ct)
    {
        var suppliesByService = (await db.Set<WashOfferingSupply>().AsNoTracking()
                .Where(s => e.OfferingIds.Contains(s.OfferingId))
                .ToListAsync(ct))
            .GroupBy(s => s.OfferingId)
            .ToDictionary(g => g.Key, g => g.Select(s => new SupplyUsage(s.ProductId, s.UnitId, s.Quantity)).ToList());
        if (suppliesByService.Count == 0) return;

        var converter = await converters.Create(suppliesByService.Values.SelectMany(s => s.Select(u => u.ProductId)), ct);
        var issueLines = SupplyExpander.Expand(e.OfferingIds, suppliesByService, converter);
        await stockIssuer.IssueForSale(e.BranchId, WashTicket.StockRefType, e.TicketId, e.Code, issueLines, ct);
    }
}
