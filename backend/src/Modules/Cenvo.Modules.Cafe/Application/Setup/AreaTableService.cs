namespace Cenvo.Modules.Cafe.Application.Setup;

/// <summary>Areas, tables and the floor plan (with open orders).</summary>
public class AreaTableService(AppDbContext db)
{
    public async Task<List<AreaDto>> FloorPlan(CancellationToken ct)
    {
        var areas = await db.Set<Area>().AsNoTracking().OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
        var tables = await db.Set<DiningTable>().AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
        var openOrderByTable = await OpenOrderByTable(ct);

        TableDto ToDto(DiningTable table)
        {
            var order = openOrderByTable.GetValueOrDefault(table.Id);
            return new TableDto(Id: table.Id, AreaId: table.AreaId, Name: table.Name, Seats: table.Seats,
                SortOrder: table.SortOrder, OpenOrderId: order?.Id, OpenOrderCode: order?.Code,
                OpenOrderTotal: order?.Total, OpenedAt: order?.CreatedAt);
        }

        return areas
            .Select(area => new AreaDto(area.Id, area.Name, area.SortOrder,
                Tables: tables.Where(t => t.AreaId == area.Id).Select(ToDto).ToList()))
            .ToList();
    }

    private record OpenOrder(Guid Id, string Code, Guid TableId, decimal Total, DateTime CreatedAt);

    /// <summary>The open order on each table (the oldest one, should a table ever have two).</summary>
    private async Task<Dictionary<Guid, OpenOrder>> OpenOrderByTable(CancellationToken ct)
    {
        var openOrders = await db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Open && o.TableId != null)
            .Select(o => new OpenOrder(o.Id, o.Code, o.TableId!.Value, o.Total, o.CreatedAt))
            .ToListAsync(ct);
        return openOrders.GroupBy(o => o.TableId).ToDictionary(g => g.Key, g => g.MinBy(o => o.CreatedAt)!);
    }

    public async Task<Guid> SaveArea(Guid? id, AreaInput input, CancellationToken ct)
    {
        var area = id == null
            ? db.Add(new Area()).Entity
            : await db.Set<Area>().FindAsync([id], ct) ?? throw new NotFoundException("khu vực", id);
        area.Update(input.Name, input.SortOrder);
        await db.SaveChangesAsync(ct);
        return area.Id;
    }

    public async Task DeleteArea(Guid id, CancellationToken ct)
    {
        if (await db.Set<DiningTable>().AnyAsync(t => t.AreaId == id, ct)) throw new BusinessException(CafeErrors.AreaHasTables);
        db.Remove(await db.Set<Area>().FindAsync([id], ct) ?? throw new NotFoundException("khu vực", id));
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SaveTable(Guid? id, TableInput input, CancellationToken ct)
    {
        if (!await db.Set<Area>().AnyAsync(a => a.Id == input.AreaId, ct)) throw new NotFoundException("khu vực", input.AreaId);
        var table = id == null
            ? db.Add(new DiningTable()).Entity
            : await db.Set<DiningTable>().FindAsync([id], ct) ?? throw new NotFoundException("bàn", id);
        table.Update(input.AreaId, input.Name, input.Seats, input.SortOrder);
        await db.SaveChangesAsync(ct);
        return table.Id;
    }

    public async Task DeleteTable(Guid id, CancellationToken ct)
    {
        if (await db.Set<CafeOrder>().AnyAsync(o => o.TableId == id && o.Status == OrderStatus.Open, ct))
            throw new BusinessException(CafeErrors.TableOccupied);
        db.Remove(await db.Set<DiningTable>().FindAsync([id], ct) ?? throw new NotFoundException("bàn", id));
        await db.SaveChangesAsync(ct);
    }
}
