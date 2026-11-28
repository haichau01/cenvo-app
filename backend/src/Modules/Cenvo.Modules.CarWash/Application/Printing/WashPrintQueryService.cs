using Cenvo.Modules.CarWash.Application.Customers;
using Cenvo.Modules.CarWash.Application.Tickets;

namespace Cenvo.Modules.CarWash.Application.Printing;

public class WashPrintQueryService(
    AppDbContext db,
    TicketQueryService tickets,
    IBranchDirectory branches,
    ICashierShifts shifts,
    ICurrentUser user)
{
    private static readonly BranchContact NoContact = new(Name: "", Address: null, Phone: null);

    /// <summary>Slip of a ticket of the current branch (the ticket lookup is branch-filtered).</summary>
    public async Task<WashTicketReceiptDto> Ticket(Guid ticketId, CancellationToken ct)
    {
        var ticket = await tickets.Get(ticketId, ct);
        var source = await db.Set<WashTicket>().AsNoTracking()
            .Where(t => t.Id == ticketId)
            .Select(t => new { t.BranchId, t.ShiftId })
            .FirstAsync(ct);

        var store = await branches.Contact(source.BranchId, ct) ?? NoContact;
        var cashier = source.ShiftId is { } shiftId ? await shifts.CashierName(shiftId, ct) : null;
        return new WashTicketReceiptDto(Ticket: ticket, Store: store, CashierName: cashier);
    }

    /// <summary>
    /// Receipt of a sold package. Memberships are chain-wide (not branch-filtered), but the receipt is a money document
    /// of the selling branch's till, so only that branch can print it; others get 404 like any other branch's document.
    /// </summary>
    public async Task<MembershipReceiptDto> Membership(Guid membershipId, CancellationToken ct)
    {
        var sale = await db.Set<CustomerMembership>().AsNoTracking()
                       .Where(m => m.Id == membershipId && m.SoldBranchId == user.BranchId)
                       .Select(m => new
                       {
                           m.CustomerId, m.SoldBranchId, m.ShiftId, m.CashReceived, m.ChangeAmount,
                           Payments = m.Payments.Select(p => new TicketPaymentDto(p.Method, p.Amount, p.Reference)).ToList(),
                       })
                       .FirstOrDefaultAsync(ct)
                   ?? throw new NotFoundException("gói thành viên", membershipId);

        var membership = await db.MembershipsOf([sale.CustomerId], DateTime.UtcNow).FirstAsync(m => m.Id == membershipId, ct);
        var customer = await db.Set<Customer>().AsNoTracking()
            .Where(c => c.Id == sale.CustomerId)
            .Select(c => new { c.Name, c.Phone })
            .FirstAsync(ct);
        var store = await branches.Contact(sale.SoldBranchId, ct) ?? NoContact;
        var cashier = await shifts.CashierName(sale.ShiftId, ct);

        return new MembershipReceiptDto(
            Membership: membership, CustomerName: customer.Name, CustomerPhone: customer.Phone, Payments: sale.Payments,
            CashReceived: sale.CashReceived, ChangeAmount: sale.ChangeAmount, Store: store, CashierName: cashier);
    }
}
