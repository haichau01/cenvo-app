namespace Cenvo.Modules.Inventory.Application.Sales;

public class SaleCostQueryService(AppDbContext db) : ISaleCostQuery
{
    public IQueryable<SaleCost> PostedCosts(string refType) =>
        db.Set<StockDocument>().AsNoTracking()
            .Where(d => d.RefType == refType && d.RefId != null && d.Status == DocumentStatus.Posted)
            .GroupBy(d => d.RefId!.Value)
            .Select(g => new SaleCost { RefId = g.Key, Cost = g.SelectMany(d => d.Lines).Sum(l => l.Amount) });
}
