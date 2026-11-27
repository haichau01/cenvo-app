namespace Cenvo.Modules.Cafe.Application.Orders;

public class OrderQueryService(AppDbContext db)
{
    public Task<List<OrderDto>> OpenOrders(CancellationToken ct) =>
        ToDtos(db.Set<CafeOrder>().Where(o => o.Status == OrderStatus.Open).OrderBy(o => o.CreatedAt), ct);

    public Task<List<OrderDto>> History(OrderHistoryFilter filter, CancellationToken ct)
    {
        var from = VnTime.StartOfDayUtc(filter.From);
        var to = VnTime.StartOfDayUtc(filter.To.AddDays(1));
        var query = db.Set<CafeOrder>().Where(o => o.Status != OrderStatus.Open && o.CreatedAt >= from && o.CreatedAt < to);
        if (filter.Status != null) query = query.Where(o => o.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(o => o.Code.Contains(filter.Search));
        return ToDtos(query.OrderByDescending(o => o.CreatedAt).Take(Limits.MaxListSize), ct);
    }

    public async Task<OrderDto> Get(Guid id, CancellationToken ct) =>
        (await ToDtos(db.Set<CafeOrder>().Where(o => o.Id == id), ct)).FirstOrDefault()
        ?? throw new NotFoundException("hóa đơn", id);

    private async Task<List<OrderDto>> ToDtos(IQueryable<CafeOrder> query, CancellationToken ct)
    {
        var orders = await query.AsNoTracking().WithDetails().ToListAsync(ct);

        var tableIds = orders.Where(o => o.TableId != null).Select(o => o.TableId!.Value).Distinct().ToList();
        var tableNames = await db.Set<DiningTable>()
            .IgnoreQueryFilters() // soft-deleted tables still show their name on old orders; ids come from branch-filtered orders
            .Where(t => tableIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);
        var shiftIds = orders.Where(o => o.ShiftId != null).Select(o => o.ShiftId!.Value).Distinct().ToList();
        var shiftCodes = await db.Set<Shift>().Where(s => shiftIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Code, ct);

        return orders.Select(o => o.ToDto(
            o.TableId == null ? null : tableNames.GetValueOrDefault(o.TableId.Value),
            o.ShiftId == null ? null : shiftCodes.GetValueOrDefault(o.ShiftId.Value))).ToList();
    }
}
