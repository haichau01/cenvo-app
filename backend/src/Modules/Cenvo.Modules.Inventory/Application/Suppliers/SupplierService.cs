namespace Cenvo.Modules.Inventory.Application.Suppliers;

public record SupplierInput(string Code, string Name, string? Phone, string? Address, string? TaxCode);
public record SupplierDto(Guid Id, string Code, string Name, string? Phone, string? Address, string? TaxCode);

public class SupplierService(AppDbContext db)
{
    public Task<List<SupplierDto>> List(CancellationToken ct) =>
        db.Set<Supplier>().AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Code, s.Name, s.Phone, s.Address, s.TaxCode)).ToListAsync(ct);

    public async Task<Guid> Save(Guid? id, SupplierInput input, CancellationToken ct)
    {
        var supplier = id == null
            ? db.Add(new Supplier()).Entity
            : await db.Set<Supplier>().FindAsync([id], ct) ?? throw new NotFoundException("nhà cung cấp", id);
        supplier.Code = input.Code.Trim();
        supplier.Name = input.Name.Trim();
        supplier.Phone = input.Phone;
        supplier.Address = input.Address;
        supplier.TaxCode = input.TaxCode;
        await db.SaveChangesAsync(ct);
        return supplier.Id;
    }
}
