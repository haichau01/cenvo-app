namespace Cenvo.Modules.Inventory.Application.Warehouses;

public record WarehouseInput(string Code, string Name, bool IsDefault);
public record WarehouseDto(Guid Id, Guid BranchId, string Code, string Name, bool IsDefault);

public class WarehouseService(AppDbContext db)
{
    /// <summary>Warehouses of the current branch; <paramref name="allBranches"/> = all branches (destination picker for transfers).</summary>
    public Task<List<WarehouseDto>> List(bool allBranches, CancellationToken ct)
    {
        var query = allBranches
            ? db.Set<Warehouse>().IgnoreQueryFilters().Where(w => !w.IsDeleted)
            : db.Set<Warehouse>();
        return query.AsNoTracking().OrderByDescending(w => w.IsDefault).ThenBy(w => w.Code)
            .Select(w => new WarehouseDto(w.Id, w.BranchId, w.Code, w.Name, w.IsDefault)).ToListAsync(ct);
    }

    public async Task<Guid> Save(Guid? id, WarehouseInput input, CancellationToken ct)
    {
        var warehouse = id == null
            ? db.Add(new Warehouse()).Entity
            : await db.Set<Warehouse>().FindAsync([id], ct) ?? throw new NotFoundException("kho", id);
        warehouse.Code = input.Code.Trim();
        warehouse.Name = input.Name.Trim();
        warehouse.IsDefault = input.IsDefault;

        if (input.IsDefault)
        {
            var others = await db.Set<Warehouse>().Where(w => w.Id != warehouse.Id && w.IsDefault).ToListAsync(ct);
            others.ForEach(w => w.IsDefault = false);
        }
        await db.SaveChangesAsync(ct);
        return warehouse.Id;
    }

    public async Task<Guid> DefaultWarehouseId(Guid branchId, CancellationToken ct) =>
        await db.Set<Warehouse>().IgnoreQueryFilters() // called from event handlers; the branch is passed explicitly
            .Where(w => w.BranchId == branchId && !w.IsDeleted)
            .OrderByDescending(w => w.IsDefault).Select(w => (Guid?)w.Id).FirstOrDefaultAsync(ct)
        ?? throw new BusinessException(InventoryErrors.NoWarehouse);
}
