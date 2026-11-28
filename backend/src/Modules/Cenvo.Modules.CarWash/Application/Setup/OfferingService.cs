namespace Cenvo.Modules.CarWash.Application.Setup;

/// <summary>Maintains the car wash price list (managers).</summary>
public class OfferingService(AppDbContext db)
{
    public async Task<Guid> Save(Guid? id, OfferingInput input, CancellationToken ct)
    {
        await EnsureCodeFree(id, input.Code, ct);
        await EnsureSuppliesExist(input.Supplies, ct);

        var offering = id == null
            ? db.Add(new WashOffering()).Entity
            : await db.Set<WashOffering>().Include(o => o.Prices).Include(o => o.Supplies).FirstOrDefaultAsync(o => o.Id == id, ct)
              ?? throw new NotFoundException("dịch vụ", id);

        offering.Update(input.Code, input.Name, input.DurationMinutes, input.CommissionType, input.CommissionValue,
            input.SortOrder, input.IsActive);
        offering.SetPrices(input.Prices.Select(p => (p.VehicleType, p.Price)));
        offering.SetSupplies(input.Supplies.Select(s => (s.ProductId, s.UnitId, s.Quantity)));
        await db.SaveChangesAsync(ct);
        return offering.Id;
    }

    /// <summary>Soft delete: old tickets keep the captured name and price.</summary>
    public async Task Delete(Guid id, CancellationToken ct)
    {
        var offering = await db.Set<WashOffering>().FindAsync([id], ct) ?? throw new NotFoundException("dịch vụ", id);
        db.Remove(offering);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeFree(Guid? id, string code, CancellationToken ct)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (await db.Set<WashOffering>().AnyAsync(o => o.Code == normalized && o.Id != id, ct))
            throw new BusinessException(CarWashErrors.ServiceCodeTaken);
    }

    private async Task EnsureSuppliesExist(OfferingSupplyInput[] supplies, CancellationToken ct)
    {
        var ids = supplies.Select(s => s.ProductId).Distinct().ToList();
        var found = await db.Set<Product>().CountAsync(p => ids.Contains(p.Id), ct);
        if (found != ids.Count) throw new NotFoundException("hóa chất / vật tư", string.Join(", ", ids));
    }
}
