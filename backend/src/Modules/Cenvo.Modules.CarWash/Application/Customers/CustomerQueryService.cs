using Cenvo.Modules.CarWash.Application.Tickets;

namespace Cenvo.Modules.CarWash.Application.Customers;

/// <summary>
/// Customers and vehicles are shared by the chain; tickets (visits) are filtered to the current branch.
/// </summary>
public class CustomerQueryService(AppDbContext db)
{
    public const int SearchLimit = 50;
    public const int RecentTicketCount = 10;

    /// <param name="q">Part of the name, phone number or licence plate.</param>
    public Task<List<CustomerListItemDto>> Search(string? q, CancellationToken ct)
    {
        var text = q?.Trim() ?? "";
        var digits = new string(text.Where(char.IsDigit).ToArray());
        var plate = PlateNumber.SearchPart(text);

        return db.Set<Customer>().AsNoTracking()
            .Where(customer => text == ""
                               || EF.Functions.ILike(customer.Name, $"%{text}%")
                               || (digits != "" && customer.Phone.Contains(digits))
                               || (plate != "" && db.Set<Vehicle>().Any(v => v.CustomerId == customer.Id && v.Plate.Contains(plate))))
            .OrderBy(customer => customer.Name)
            .Take(SearchLimit)
            .Select(customer => new CustomerListItemDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Plates = db.Set<Vehicle>().Where(v => v.CustomerId == customer.Id).OrderBy(v => v.Plate).Select(v => v.Plate).ToList(),
            })
            .ToListAsync(ct);
    }

    public async Task<CustomerDetailDto> Get(Guid id, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException("khách hàng", id);
        var vehicles = await db.Set<Vehicle>().AsNoTracking().Where(v => v.CustomerId == id).OrderBy(v => v.Plate)
            .Select(CustomerQueries.ToVehicleDto).ToListAsync(ct);
        var memberships = await db.MembershipsOf([id], DateTime.UtcNow).Take(Limits.MaxListSize).ToListAsync(ct);
        var tickets = await RecentTickets(db.Set<WashTicket>().Where(t => t.CustomerId == id), ct);
        return new CustomerDetailDto(
            Id: customer.Id, Name: customer.Name, Phone: customer.Phone, Note: customer.Note,
            Vehicles: vehicles, Memberships: memberships, RecentTickets: tickets);
    }

    /// <returns>Null for a plate never seen before (a new vehicle).</returns>
    public async Task<VehicleLookupDto?> LookupPlate(string plate, CancellationToken ct)
    {
        var normalized = PlateNumber.Normalize(plate);
        var vehicle = await db.Set<Vehicle>().AsNoTracking().FirstOrDefaultAsync(v => v.Plate == normalized, ct);
        if (vehicle == null) return null;

        var customer = vehicle.CustomerId == null
            ? null
            : await db.Set<Customer>().AsNoTracking().Where(c => c.Id == vehicle.CustomerId)
                .Select(c => new CustomerRef(c.Id, c.Name, c.Phone)).FirstOrDefaultAsync(ct);
        var memberships = customer == null
            ? []
            : await db.MembershipsOf([customer.Id], DateTime.UtcNow, usableOnly: true).ToListAsync(ct);
        var visits = db.Set<WashTicket>().Where(t => t.VehicleId == vehicle.Id);
        var openTicket = await visits.InProgress().OrderByDescending(t => t.CreatedAt).Select(TicketQueries.ToHistory)
            .FirstOrDefaultAsync(ct);

        return new VehicleLookupDto(
            Vehicle: vehicle.ToDto(), Customer: customer, Memberships: memberships,
            RecentTickets: await RecentTickets(visits, ct), OpenTicket: openTicket);
    }

    private static Task<List<TicketHistoryDto>> RecentTickets(IQueryable<WashTicket> tickets, CancellationToken ct) =>
        tickets.AsNoTracking().OrderByDescending(t => t.CreatedAt).Take(RecentTicketCount).Select(TicketQueries.ToHistory)
            .ToListAsync(ct);
}
