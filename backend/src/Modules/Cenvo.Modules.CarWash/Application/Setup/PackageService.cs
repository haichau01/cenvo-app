namespace Cenvo.Modules.CarWash.Application.Setup;

/// <summary>Membership packages on sale (managers maintain them, cashiers sell them).</summary>
public class PackageService(AppDbContext db)
{
    public Task<List<PackageDto>> List(bool includeInactive, CancellationToken ct) =>
        (from package in db.Set<MembershipPackage>().AsNoTracking()
         join offering in db.Set<WashOffering>() on package.OfferingId equals offering.Id
         where includeInactive || (package.IsActive && offering.IsActive)
         orderby package.Name
         select new PackageDto
         {
             Id = package.Id,
             Code = package.Code,
             Name = package.Name,
             OfferingId = package.OfferingId,
             OfferingName = offering.Name,
             VehicleType = package.VehicleType,
             Price = package.Price,
             Uses = package.Uses,
             ValidDays = package.ValidDays,
             IsActive = package.IsActive,
         })
        .Take(Limits.MaxListSize)
        .ToListAsync(ct);

    public async Task<Guid> Save(Guid? id, PackageInput input, CancellationToken ct)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        if (await db.Set<MembershipPackage>().AnyAsync(p => p.Code == code && p.Id != id, ct))
            throw new BusinessException(CarWashErrors.PackageCodeTaken);
        if (!await db.Set<WashOffering>().AnyAsync(o => o.Id == input.OfferingId, ct))
            throw new NotFoundException("dịch vụ", input.OfferingId);

        var package = id == null
            ? db.Add(new MembershipPackage()).Entity
            : await db.Set<MembershipPackage>().FindAsync([id], ct) ?? throw new NotFoundException("gói thành viên", id);
        package.Update(input.Code, input.Name, input.OfferingId, input.VehicleType, input.Price, input.Uses, input.ValidDays,
            input.IsActive);
        await db.SaveChangesAsync(ct);
        return package.Id;
    }
}
