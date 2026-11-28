namespace Cenvo.Modules.CarWash.Application.Shifts;

/// <summary>Adds car wash tickets and membership sales to the cashier shift reconciliation (cancelled ones were refunded).</summary>
public class WashShiftTakingsService(AppDbContext db) : IShiftTakingsSource
{
    public const string TicketsSource = "Rửa xe";
    public const string MembershipsSource = "Gói thành viên";

    public async Task<IReadOnlyList<ShiftTakings>> ForShift(Guid shiftId, CancellationToken ct)
    {
        var tickets = await db.Set<WashTicket>().AsNoTracking()
            .Where(t => t.ShiftId == shiftId && t.Status != WashStatus.Cancelled)
            .Select(t => new PaidDocument
            {
                Total = t.Total,
                Payments = t.Payments.Select(p => new ShiftTakingsPayment(p.Method, p.Amount)).ToList(),
            })
            .ToListAsync(ct);
        var memberships = await db.Set<CustomerMembership>().AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == MembershipStatus.Active)
            .Select(m => new PaidDocument
            {
                Total = m.Price,
                Payments = m.Payments.Select(p => new ShiftTakingsPayment(p.Method, p.Amount)).ToList(),
            })
            .ToListAsync(ct);
        return [Summarize(TicketsSource, tickets), Summarize(MembershipsSource, memberships)];
    }

    private class PaidDocument
    {
        public required decimal Total { get; init; }
        public required List<ShiftTakingsPayment> Payments { get; init; }
    }

    private static ShiftTakings Summarize(string source, List<PaidDocument> documents) =>
        new(Source: source, Count: documents.Count, Revenue: documents.Sum(d => d.Total),
            Payments: documents.SelectMany(d => d.Payments).ToList());
}
