namespace Cenvo.Modules.Catalog.Application.Units;

public record UnitDto(Guid Id, string Name);

public class UnitService(AppDbContext db)
{
    public Task<List<UnitDto>> List(CancellationToken ct) =>
        db.Set<Unit>().AsNoTracking().OrderBy(u => u.Name).Select(u => new UnitDto(u.Id, u.Name)).ToListAsync(ct);

    public async Task<Guid> Save(Guid? id, string name, CancellationToken ct)
    {
        var unit = id == null
            ? db.Add(new Unit()).Entity
            : await db.Set<Unit>().FindAsync([id], ct) ?? throw new NotFoundException("đơn vị", id);
        unit.Name = name.Trim();
        await db.SaveChangesAsync(ct);
        return unit.Id;
    }
}
