namespace Cenvo.Modules.Cafe.Application.Setup;

/// <summary>Areas, tables and the floor plan (with open orders).</summary>
public class AreaTableService(AppDbContext db)
{
    public async Task<List<AreaDto>> FloorPlan(CancellationToken ct)
    {
        var areas = await db.Set<Area>().AsNoTracking().OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
        var tables = await db.Set<DiningTable>().AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
        var openOrders = await db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Open && o.TableId != null)
            .Select(o => new { o.Id, o.Code, TableId = o.TableId!.Value, o.Total, o.CreatedAt })
            .ToListAsync(ct);
        var orderByTable = openOrders.GroupBy(o => o.TableId).ToDictionary(g => g.Key, g => g.MinBy(o => o.CreatedAt)!);

        return areas.Select(a => new AreaDto(a.Id, a.Name, a.SortOrder, tables.Where(t => t.AreaId == a.Id).Select(t =>
        {
            var order = orderByTable.GetValueOrDefault(t.Id);
            return new TableDto(t.Id, t.AreaId, t.Name, t.Seats, t.SortOrder, order?.Id, order?.Code, order?.Total, order?.CreatedAt);
        }).ToList())).ToList();
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
