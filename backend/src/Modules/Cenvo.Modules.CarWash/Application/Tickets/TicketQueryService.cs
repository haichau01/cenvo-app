namespace Cenvo.Modules.CarWash.Application.Tickets;

public class TicketQueryService(AppDbContext db, IStaffDirectory staff, ICurrentUser user)
{
    /// <summary>Vehicles at the shop + those delivered today (the "Đã giao" column).</summary>
    public Task<List<QueueTicketDto>> Queue(CancellationToken ct)
    {
        var todayUtc = VnTime.StartOfDayUtc(VnTime.Today);

        return (from ticket in db.Set<WashTicket>().AsNoTracking().OnQueueBoard(deliveredSinceUtc: todayUtc)
                orderby ticket.CreatedAt
                select new QueueTicketDto
                {
                    Id = ticket.Id,
                    Code = ticket.Code,
                    Plate = ticket.Plate,
                    VehicleType = ticket.VehicleType,
                    VehicleDescription = ticket.VehicleDescription,
                    CustomerName = ticket.CustomerName,
                    Status = ticket.Status,
                    IsPaid = ticket.PaidAt != null,
                    Total = ticket.Total,
                    EstimatedMinutes = ticket.Lines.Sum(line => line.DurationMinutes),
                    Services = ticket.Lines.OrderBy(line => line.SortOrder).Select(line => line.Name).ToList(),
                    Staff = ticket.Staff.OrderBy(member => member.FullName).Select(member => member.FullName).ToList(),
                    PhotoCount = ticket.Photos.Count,
                    CreatedAt = ticket.CreatedAt,
                    StartedAt = ticket.StartedAt,
                    CompletedAt = ticket.CompletedAt,
                    DeliveredAt = ticket.DeliveredAt,
                })
            .Take(Limits.MaxListSize)
            .ToListAsync(ct);
    }

    public async Task<TicketDto> Get(Guid id, CancellationToken ct) =>
        await (from ticket in db.Set<WashTicket>().AsNoTracking()
               where ticket.Id == id
               select new TicketDto
               {
                   Id = ticket.Id,
                   Code = ticket.Code,
                   VehicleId = ticket.VehicleId,
                   Plate = ticket.Plate,
                   VehicleType = ticket.VehicleType,
                   VehicleDescription = ticket.VehicleDescription,
                   CustomerId = ticket.CustomerId,
                   CustomerName = ticket.CustomerName,
                   CustomerPhone = ticket.CustomerPhone,
                   Note = ticket.Note,
                   Status = ticket.Status,
                   IsPaid = ticket.PaidAt != null,
                   IsEditable = (ticket.Status == WashStatus.Waiting || ticket.Status == WashStatus.Washing) && ticket.PaidAt == null,
                   Subtotal = ticket.Subtotal,
                   MembershipCovered = ticket.MembershipCovered,
                   Total = ticket.Total,
                   CashReceived = ticket.CashReceived,
                   ChangeAmount = ticket.ChangeAmount,
                   CreatedAt = ticket.CreatedAt,
                   StartedAt = ticket.StartedAt,
                   CompletedAt = ticket.CompletedAt,
                   DeliveredAt = ticket.DeliveredAt,
                   PaidAt = ticket.PaidAt,
                   CancelledAt = ticket.CancelledAt,
                   CancelReason = ticket.CancelReason,
                   Lines = ticket.Lines.OrderBy(line => line.SortOrder)
                       .Select(line => new TicketLineDto(line.Id, line.OfferingId, line.Name, line.Price, line.Amount,
                           line.MembershipId, line.DurationMinutes))
                       .ToList(),
                   Staff = ticket.Staff.OrderBy(member => member.FullName)
                       .Select(member => new TicketStaffDto(member.UserId, member.FullName, member.Commission)).ToList(),
                   Payments = ticket.Payments.Select(payment => new TicketPaymentDto(payment.Method, payment.Amount, payment.Reference))
                       .ToList(),
                   Photos = ticket.Photos.OrderBy(photo => photo.TakenAt).Select(photo => new TicketPhotoDto(photo.Id, photo.TakenAt))
                       .ToList(),
               })
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("phiếu rửa xe", id);

    /// <param name="filter">Created between the dates (Vietnam time); Q matches code, plate or customer name.</param>
    public Task<List<TicketHistoryDto>> History(TicketHistoryFilter filter, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(filter.From);
        var toUtc = VnTime.StartOfDayUtc(filter.To.AddDays(1));
        var text = filter.Q?.Trim() ?? "";
        var plate = PlateNumber.SearchPart(text);

        return db.Set<WashTicket>().AsNoTracking()
            .Where(ticket => ticket.CreatedAt >= fromUtc && ticket.CreatedAt < toUtc)
            .Where(ticket => text == ""
                             || ticket.Code.Contains(text.ToUpper())
                             || (plate != "" && ticket.Plate.Contains(plate))
                             || EF.Functions.ILike(ticket.CustomerName ?? "", $"%{text}%"))
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(Limits.MaxListSize)
            .Select(TicketQueries.ToHistory)
            .ToListAsync(ct);
    }

    public async Task<List<WasherDto>> Washers(CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(CarWashErrors.NoBranch);
        var washers = await staff.InBranch(branchId, Roles.Washer, ct);
        return washers.Select(w => new WasherDto(w.UserId, w.FullName)).ToList();
    }
}
