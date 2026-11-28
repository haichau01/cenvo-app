using System.Linq.Expressions;

namespace Cenvo.Modules.CarWash.Application.Customers;

public static class CustomerQueries
{
    public static readonly Expression<Func<Vehicle, VehicleDto>> ToVehicleDto = vehicle => new VehicleDto
    {
        Id = vehicle.Id,
        Plate = vehicle.Plate,
        VehicleType = vehicle.VehicleType,
        Description = vehicle.Description,
    };

    private static readonly Func<Vehicle, VehicleDto> ToVehicleDtoCompiled = ToVehicleDto.Compile();

    public static VehicleDto ToDto(this Vehicle vehicle) => ToVehicleDtoCompiled(vehicle);

    /// <summary>Memberships of the customers with the service name; usable ones first, then newest.</summary>
    public static IQueryable<MembershipDto> MembershipsOf(this AppDbContext db, IReadOnlyCollection<Guid> customerIds, DateTime now,
        bool usableOnly = false) =>
        from membership in db.Set<CustomerMembership>().AsNoTracking()
        join offering in db.Set<WashOffering>().IgnoreQueryFilters() // a deleted service keeps its sold packages; not branch-scoped
            on membership.OfferingId equals offering.Id
        where customerIds.Contains(membership.CustomerId)
        let isUsable = membership.Status == MembershipStatus.Active && membership.RemainingUses > 0 && membership.ExpiresAt > now
        where !usableOnly || isUsable
        orderby isUsable descending, membership.PaidAt descending
        select new MembershipDto
        {
            Id = membership.Id,
            Code = membership.Code,
            CustomerId = membership.CustomerId,
            PackageName = membership.PackageName,
            OfferingId = membership.OfferingId,
            OfferingName = offering.Name,
            VehicleType = membership.VehicleType,
            TotalUses = membership.TotalUses,
            RemainingUses = membership.RemainingUses,
            Price = membership.Price,
            PaidAt = membership.PaidAt,
            ExpiresAt = membership.ExpiresAt,
            Status = membership.Status,
            IsUsable = isUsable,
        };
}
