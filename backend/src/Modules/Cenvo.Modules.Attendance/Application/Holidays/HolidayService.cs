namespace Cenvo.Modules.Attendance.Application.Holidays;

/// <summary>The owner keeps the holiday list (chain-wide); managers and staff read it.</summary>
public class HolidayService(AppDbContext db)
{
    public Task<List<HolidayDto>> OfYear(int year, CancellationToken ct) =>
        db.Between(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), ct);

    public async Task<Guid> Save(Guid? id, HolidayInput input, CancellationToken ct)
    {
        var holiday = id is { } existing
            ? await db.Set<Holiday>().FirstOrDefaultAsync(h => h.Id == existing, ct) ?? throw new NotFoundException("ngày lễ", existing)
            : db.Add(new Holiday()).Entity;
        holiday.Update(input.Date, input.Name, input.Multiplier);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            throw new BusinessException(AttendanceErrors.HolidayDateTaken); // unique date
        }
        return holiday.Id;
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        var holiday = await db.Set<Holiday>().FirstOrDefaultAsync(h => h.Id == id, ct) ?? throw new NotFoundException("ngày lễ", id);
        db.Remove(holiday); // soft delete
        await db.SaveChangesAsync(ct);
    }
}
