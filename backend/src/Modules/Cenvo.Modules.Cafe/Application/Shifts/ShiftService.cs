namespace Cenvo.Modules.Cafe.Application.Shifts;

public record OpenShiftInput(decimal OpeningCash);
public record CloseShiftInput(decimal CountedCash, string? Note);

public record ShiftDto(Guid Id, string Code, string CashierName, ShiftStatus Status, DateTime OpenedAt, DateTime? ClosedAt,
    decimal OpeningCash, decimal? ExpectedCash, decimal? CountedCash);

public record ShiftSummary(Guid Id, string Code, string CashierName, ShiftStatus Status, DateTime OpenedAt, DateTime? ClosedAt,
    decimal OpeningCash, int OrderCount, int CancelledCount, decimal Revenue, decimal Discount,
    Dictionary<PaymentMethod, decimal> ByMethod, decimal ExpectedCash, decimal? CountedCash, decimal? Difference, string? Note,
    List<ShiftTakings> OtherTakings);

public class ShiftService(
    AppDbContext db,
    IDocumentNumberGenerator numbers,
    ICurrentUser user,
    IEnumerable<IShiftTakingsSource> takingsSources) : ICashierShifts
{
    private const string CodePrefix = "CA";

    public Task<Shift?> CurrentShift(CancellationToken ct) =>
        db.Set<Shift>().FirstOrDefaultAsync(s => s.CashierId == user.UserId && s.Status == ShiftStatus.Open, ct);

    public Task<Guid?> CurrentShiftId(CancellationToken ct) =>
        db.Set<Shift>().AsNoTracking()
            .Where(s => s.CashierId == user.UserId && s.Status == ShiftStatus.Open)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

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
        var closedOrders = await ClosedOrders(shiftId, ct);
        var paid = closedOrders.Where(o => o.Status == OrderStatus.Paid).ToList();
        var otherTakings = await OtherTakings(shiftId, ct);
        var byMethod = TotalByMethod(paid, otherTakings);
        // A closed shift keeps the expected cash it was closed with; an open shift computes it live.
        var expectedCash = shift.ExpectedCash ?? shift.OpeningCash + byMethod[PaymentMethod.Cash];

        return new ShiftSummary(
            Id: shift.Id,
            Code: shift.Code,
            CashierName: shift.CashierName,
            Status: shift.Status,
            OpenedAt: shift.OpenedAt,
            ClosedAt: shift.ClosedAt,
            OpeningCash: shift.OpeningCash,
            OrderCount: paid.Count,
            CancelledCount: closedOrders.Count(o => o.Status == OrderStatus.Cancelled),
            Revenue: paid.Sum(o => o.Total),
            Discount: paid.Sum(o => o.Discount),
            ByMethod: byMethod,
            ExpectedCash: expectedCash,
            CountedCash: shift.CountedCash,
            Difference: shift.CountedCash - expectedCash,
            Note: shift.Note,
            OtherTakings: otherTakings);
    }

    private record ShiftOrder(OrderStatus Status, decimal Total, decimal Discount, List<ShiftTakingsPayment> Payments);

    /// <summary>Paid and cancelled orders of the shift (open orders are not settled yet).</summary>
    private Task<List<ShiftOrder>> ClosedOrders(Guid shiftId, CancellationToken ct) =>
        db.Set<CafeOrder>().AsNoTracking()
            .Where(o => o.ShiftId == shiftId && o.Status != OrderStatus.Open)
            .Select(o => new ShiftOrder(
                o.Status,
                o.Total,
                o.DiscountAmount + o.Lines.Sum(l => l.DiscountAmount),
                o.Payments.Select(p => new ShiftTakingsPayment(p.Method, p.Amount)).ToList()))
            .ToListAsync(ct);

    /// <summary>Takings of other sales sources (car wash...). Sequential: the sources share this scope's DbContext.</summary>
    private async Task<List<ShiftTakings>> OtherTakings(Guid shiftId, CancellationToken ct)
    {
        var takings = new List<ShiftTakings>();
        foreach (var source in takingsSources) takings.AddRange(await source.ForShift(shiftId, ct));
        return takings;
    }

    /// <summary>Cafe orders + other sources; every payment method is present (0 when unused).</summary>
    private static Dictionary<PaymentMethod, decimal> TotalByMethod(List<ShiftOrder> paid, List<ShiftTakings> otherTakings)
    {
        var payments = paid.SelectMany(o => o.Payments).Concat(otherTakings.SelectMany(t => t.Payments)).ToList();
        return Enum.GetValues<PaymentMethod>().ToDictionary(
            method => method,
            method => payments.Where(p => p.Method == method).Sum(p => p.Amount));
    }
}
