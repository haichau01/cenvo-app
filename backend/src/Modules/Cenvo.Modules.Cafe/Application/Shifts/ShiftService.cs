namespace Cenvo.Modules.Cafe.Application.Shifts;

public record OpenShiftInput(decimal OpeningCash);
public record CloseShiftInput(decimal CountedCash, string? Note);

public record ShiftDto(Guid Id, string Code, string CashierName, ShiftStatus Status, DateTime OpenedAt, DateTime? ClosedAt,
    decimal OpeningCash, decimal? ExpectedCash, decimal? CountedCash);

public record ShiftSummary(Guid Id, string Code, string CashierName, ShiftStatus Status, DateTime OpenedAt, DateTime? ClosedAt,
    decimal OpeningCash, int OrderCount, int CancelledCount, decimal Revenue, decimal Discount,
    Dictionary<PaymentMethod, decimal> ByMethod, decimal ExpectedCash, decimal? CountedCash, decimal? Difference, string? Note);

public class ShiftService(AppDbContext db, IDocumentNumberGenerator numbers, ICurrentUser user)
{
    private const string CodePrefix = "CA";

    public Task<Shift?> CurrentShift(CancellationToken ct) =>
        db.Set<Shift>().FirstOrDefaultAsync(s => s.CashierId == user.UserId && s.Status == ShiftStatus.Open, ct);

    public async Task<ShiftSummary> Open(decimal openingCash, string cashierName, CancellationToken ct)
    {
        if (await CurrentShift(ct) != null) throw new BusinessException(CafeErrors.ShiftAlreadyOpen);
        var branchId = user.BranchId ?? throw new BusinessException(CafeErrors.NoBranch);
        var now = DateTime.UtcNow;

        var shift = Shift.Open(branchId, await numbers.Next(branchId, CodePrefix, now, ct), user.UserId!.Value, cashierName, openingCash, now);
        db.Add(shift);
        await db.SaveChangesAsync(ct);
        return await Summary(shift.Id, ct);
    }

    public async Task<ShiftSummary> Close(CloseShiftInput input, CancellationToken ct)
    {
        var shift = await CurrentShift(ct) ?? throw new BusinessException(CafeErrors.NoOpenShift);
        var summary = await Summary(shift.Id, ct);
        shift.Close(summary.ExpectedCash, input.CountedCash, input.Note, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return await Summary(shift.Id, ct);
    }

    public Task<List<ShiftDto>> List(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var fromUtc = VnTime.StartOfDayUtc(from);
        var toUtc = VnTime.StartOfDayUtc(to.AddDays(1));
        return db.Set<Shift>().AsNoTracking().Where(s => s.OpenedAt >= fromUtc && s.OpenedAt < toUtc)
            .OrderByDescending(s => s.OpenedAt).Take(Limits.MaxListSize)
            .Select(s => new ShiftDto(s.Id, s.Code, s.CashierName, s.Status, s.OpenedAt, s.ClosedAt, s.OpeningCash,
                s.ExpectedCash, s.CountedCash))
            .ToListAsync(ct);
    }

    public async Task<ShiftSummary> Summary(Guid shiftId, CancellationToken ct)
    {
        var shift = await db.Set<Shift>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct)
                    ?? throw new NotFoundException("ca", shiftId);
        var orders = await db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.ShiftId == shiftId && o.Status != OrderStatus.Open)
            .Select(o => new
            {
                o.Status, o.Total, Discount = o.DiscountAmount + o.Lines.Sum(l => l.DiscountAmount),
                Payments = o.Payments.Select(p => new { p.Method, p.Amount }).ToList(),
            })
            .ToListAsync(ct);

        var paid = orders.Where(o => o.Status == OrderStatus.Paid).ToList();
        var byMethod = Enum.GetValues<PaymentMethod>().ToDictionary(m => m,
            m => paid.SelectMany(o => o.Payments).Where(p => p.Method == m).Sum(p => p.Amount));
        var expectedCash = shift.ExpectedCash ?? shift.OpeningCash + byMethod[PaymentMethod.Cash];

        return new ShiftSummary(shift.Id, shift.Code, shift.CashierName, shift.Status, shift.OpenedAt, shift.ClosedAt,
            shift.OpeningCash, paid.Count, orders.Count(o => o.Status == OrderStatus.Cancelled), paid.Sum(o => o.Total),
            paid.Sum(o => o.Discount), byMethod, expectedCash, shift.CountedCash, shift.CountedCash - expectedCash, shift.Note);
    }
}
