namespace Cenvo.Modules.CarWash.Application.Reports;

/// <summary>Commission earned per washer (by completion date, Vietnam time). Cancelled tickets earn nothing.</summary>
public class CommissionQueryService(AppDbContext db)
{
    public async Task<CommissionReport> Summary(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));

        var staff = await (from member in db.Set<WashTicketStaff>().AsNoTracking()
                           join ticket in db.Set<WashTicket>() on member.TicketId equals ticket.Id // branch filter comes from WashTicket
                           where ticket.CompletedAt >= fromUtc && ticket.CompletedAt < toUtc
                                 && ticket.Status != WashStatus.Cancelled
                           group member by member.UserId into byStaff
                           orderby byStaff.Sum(m => m.Commission) descending
                           select new StaffCommissionDto
                           {
                               UserId = byStaff.Key,
                               FullName = byStaff.Max(m => m.FullName)!, // same person; a renamed account keeps one row
                               TicketCount = byStaff.Count(),
                               Commission = byStaff.Sum(m => m.Commission),
                           })
            .Take(Limits.MaxListSize)
            .ToListAsync(ct);
        return new CommissionReport(From: from, To: to, Total: staff.Sum(s => s.Commission), Staff: staff);
    }
}
