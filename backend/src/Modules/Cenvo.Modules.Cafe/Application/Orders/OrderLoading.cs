namespace Cenvo.Modules.Cafe.Application.Orders;

/// <summary>Shared queries for editing an order (loads the full line tree, tracked).</summary>
internal static class OrderLoading
{
    public static IQueryable<CafeOrder> WithDetails(this IQueryable<CafeOrder> orders) =>
        orders.Include(o => o.Lines).ThenInclude(l => l.Toppings).Include(o => o.Payments).AsSplitQuery();

    public static async Task<CafeOrder> LoadOrder(this AppDbContext db, Guid id, CancellationToken ct) =>
        await db.Set<CafeOrder>().WithDetails().FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new NotFoundException("hóa đơn", id);

    public static async Task<CafeOrder?> FindOpenOrderAtTable(this AppDbContext db, Guid tableId, CancellationToken ct)
    {
        var id = await db.Set<CafeOrder>().Where(o => o.TableId == tableId && o.Status == OrderStatus.Open)
            .Select(o => (Guid?)o.Id).FirstOrDefaultAsync(ct);
        return id == null ? null : await db.LoadOrder(id.Value, ct);
    }

    public static async Task EnsureTableExists(this AppDbContext db, Guid tableId, CancellationToken ct)
    {
        if (!await db.Set<DiningTable>().AnyAsync(t => t.Id == tableId, ct)) throw new NotFoundException("bàn", tableId);
    }
}
