namespace Cenvo.Modules.CarWash.Application.Tickets;

/// <summary>Loads a ticket with its whole tree, tracked, for editing.</summary>
internal static class TicketLoading
{
    public static async Task<WashTicket> LoadTicket(this AppDbContext db, Guid id, CancellationToken ct) =>
        await db.Set<WashTicket>()
            .Include(t => t.Lines).Include(t => t.Staff).Include(t => t.Payments).Include(t => t.Photos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new NotFoundException("phiếu rửa xe", id);

    /// <summary>Gives back the membership uses of the given lines (service removed / ticket cancelled).</summary>
    public static async Task RestoreMembershipUses(this AppDbContext db, IEnumerable<WashTicketLine> lines, CancellationToken ct)
    {
        var ids = lines.Where(l => l.MembershipId != null).Select(l => l.MembershipId!.Value).ToList();
        if (ids.Count == 0) return;
        var memberships = await db.Set<CustomerMembership>().Where(m => ids.Contains(m.Id)).ToListAsync(ct);
        foreach (var membership in memberships) membership.Restore();
    }
}
