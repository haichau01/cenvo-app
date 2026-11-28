using Cenvo.Modules.CarWash.Application.Customers;

namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <summary>Taking a vehicle in and editing what will be done to it (services, memberships, staff).</summary>
public class TicketIntakeService(
    AppDbContext db,
    IDocumentNumberGenerator numbers,
    IStaffDirectory staffDirectory,
    TicketQueryService queries,
    IWashNotifier notifier,
    ICurrentUser user)
{
    public async Task<TicketDto> Create(CreateTicketInput input, CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(CarWashErrors.NoBranch);
        if (input.Services.Length == 0) throw new BusinessException(CarWashErrors.ServicesRequired);

        var vehicle = await FindOrRegisterVehicle(input.Plate, input.VehicleType, ct);
        // No phone typed for a known car → the ticket belongs to its last known owner (so their membership can be used).
        var customer = await db.FindOrCreateByPhone(input.CustomerPhone, input.CustomerName, ct)
                       ?? await KnownOwner(vehicle, ct);
        await EnsureNoOpenTicket(vehicle.Id, ct);
        vehicle.UpdateAtIntake(input.VehicleType, input.VehicleDescription, customer?.Id);

        var now = DateTime.UtcNow;
        var code = await numbers.Next(branchId, WashTicket.CodePrefix, now, ct);
        var ticket = WashTicket.Open(branchId, code, vehicle, customer, input.CustomerName, input.Note);
        db.Add(ticket);
        foreach (var service in input.Services) await AddService(ticket, service, now, ct);
        if (input.StaffIds is { Length: > 0 }) ticket.AssignStaff(await Washers(branchId, input.StaffIds, ct));

        return await Save(ticket, ct);
    }

    public async Task<TicketDto> AddService(Guid ticketId, TicketServiceInput input, CancellationToken ct)
    {
        var ticket = await db.LoadTicket(ticketId, ct);
        await AddService(ticket, input, DateTime.UtcNow, ct);
        return await Save(ticket, ct);
    }

    public async Task<TicketDto> RemoveService(Guid ticketId, Guid lineId, CancellationToken ct)
    {
        var ticket = await db.LoadTicket(ticketId, ct);
        var removed = ticket.RemoveService(lineId);
        await db.RestoreMembershipUses([removed], ct);
        return await Save(ticket, ct);
    }

    public async Task<TicketDto> AssignStaff(Guid ticketId, AssignStaffInput input, CancellationToken ct)
    {
        var ticket = await db.LoadTicket(ticketId, ct);
        ticket.AssignStaff(await Washers(ticket.BranchId, input.StaffIds, ct));
        return await Save(ticket, ct);
    }

    private async Task AddService(WashTicket ticket, TicketServiceInput input, DateTime now, CancellationToken ct)
    {
        var offering = await db.Set<WashOffering>().Include(o => o.Prices).FirstOrDefaultAsync(o => o.Id == input.OfferingId, ct)
                       ?? throw new NotFoundException("dịch vụ", input.OfferingId);
        ticket.AddService(offering, input.MembershipId);
        if (input.MembershipId == null) return;

        var membership = await db.Set<CustomerMembership>().FindAsync([input.MembershipId], ct)
                         ?? throw new NotFoundException("gói thành viên", input.MembershipId);
        membership.Use(ticket.CustomerId!.Value, offering.Id, ticket.VehicleType, now);
    }

    private async Task<Vehicle> FindOrRegisterVehicle(string plate, VehicleType vehicleType, CancellationToken ct)
    {
        var normalized = PlateNumber.Normalize(plate);
        return await db.Set<Vehicle>().FirstOrDefaultAsync(v => v.Plate == normalized, ct)
               ?? db.Add(Vehicle.Register(normalized, vehicleType)).Entity;
    }

    private async Task<Customer?> KnownOwner(Vehicle vehicle, CancellationToken ct) =>
        vehicle.CustomerId == null ? null : await db.Set<Customer>().FindAsync([vehicle.CustomerId], ct);

    private async Task EnsureNoOpenTicket(Guid vehicleId, CancellationToken ct)
    {
        var openCode = await db.Set<WashTicket>().InProgress().Where(t => t.VehicleId == vehicleId)
            .Select(t => t.Code).FirstOrDefaultAsync(ct);
        if (openCode != null) throw new BusinessException(CarWashErrors.VehicleHasOpenTicket(openCode));
    }

    /// <summary>Only washers of the ticket's branch can be assigned (and earn commission).</summary>
    private async Task<List<StaffMember>> Washers(Guid branchId, IReadOnlyCollection<Guid> staffIds, CancellationToken ct)
    {
        var washers = await staffDirectory.InBranch(branchId, Roles.Washer, ct);
        var chosen = washers.Where(w => staffIds.Contains(w.UserId)).ToList();
        if (chosen.Count != staffIds.Distinct().Count()) throw new BusinessException(CarWashErrors.StaffNotInBranch);
        return chosen;
    }

    private async Task<TicketDto> Save(WashTicket ticket, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await notifier.TicketsChanged(ticket.BranchId, ct);
        return await queries.Get(ticket.Id, ct);
    }
}
