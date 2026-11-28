namespace Cenvo.Modules.Hr.Domain.Calculations;

public static class SalaryHistory
{
    /// <summary>The rate in force on a day: the latest one that started on or before it; null before the first rate.</summary>
    public static SalaryRate? EffectiveOn(IEnumerable<SalaryRate> rates, DateOnly day) =>
        rates.Where(r => r.EffectiveFrom <= day).MaxBy(r => r.EffectiveFrom);
}
