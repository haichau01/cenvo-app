namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <summary>Taking payment (inside the cashier's open shift) and cancelling tickets.</summary>
public class TicketCheckoutService(
    AppDbContext db,
    ICashierShifts shifts,
    TicketQueryService queries,
    IWashNotifier notifier,
    ICurrentUser user)
{
    public async Task<TicketDto> Pay(Guid id, PayTicketInput input, CancellationToken ct)
    {
        var shiftId = await shifts.CurrentShiftId(ct) ?? throw new BusinessException(CarWashErrors.ShiftNotOpen);
        var ticket = await db.LoadTicket(id, ct);
        ticket.Pay(shiftId, user.UserId, input.Payments, DateTime.UtcNow);
        return await Save(ticket, ct);
    }

    /// <summary>Membership uses come back; a washed ticket also returns its supplies to stock (domain event).</summary>
    public async Task<TicketDto> Cancel(Guid id, CancelTicketInput input, CancellationToken ct)
    {
        var ticket = await db.LoadTicket(id, ct);
        ticket.Cancel(input.Reason, byManager: user.IsManager(), DateTime.UtcNow);
        await db.RestoreMembershipUses(ticket.Lines, ct);
        return await Save(ticket, ct);
    }

    private async Task<TicketDto> Save(WashTicket ticket, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await notifier.TicketsChanged(ticket.BranchId, ct);
        return await queries.Get(ticket.Id, ct);
    }
}
