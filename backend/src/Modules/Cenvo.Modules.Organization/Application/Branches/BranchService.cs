namespace Cenvo.Modules.Organization.Application.Branches;

public class BranchService(AppDbContext db) : IBranchStockPolicy
{
    public Task<List<BranchDto>> List(CancellationToken ct) =>
        db.Set<Branch>().AsNoTracking().OrderBy(b => b.Code)
            .Select(b => new BranchDto(b.Id, b.Code, b.Name, b.Address, b.Phone, b.AllowNegativeStock))
            .ToListAsync(ct);

    public async Task<Guid> Save(Guid? id, BranchInput input, CancellationToken ct)
    {
        var branch = id == null
            ? db.Add(new Branch()).Entity
            : await db.Set<Branch>().FindAsync([id], ct) ?? throw new NotFoundException("chi nhánh", id);
        branch.Update(input.Code, input.Name, input.Address, input.Phone, input.AllowNegativeStock);
        await db.SaveChangesAsync(ct);
        return branch.Id;
    }

    public Task<bool> AllowNegativeStock(Guid branchId, CancellationToken ct) =>
        db.Set<Branch>().IgnoreQueryFilters() // soft-deleted branches still have documents that may need cancelling
            .Where(b => b.Id == branchId).Select(b => b.AllowNegativeStock).FirstOrDefaultAsync(ct);
}
