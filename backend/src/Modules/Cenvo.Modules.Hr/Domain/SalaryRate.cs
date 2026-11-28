namespace Cenvo.Modules.Hr.Domain;

/// <summary>Monthly salary + fixed monthly allowance from a month on, until the next rate. Append-only history.</summary>
public class SalaryRate : BaseEntity
{
    public Guid EmployeeId { get; set; }
    /// <summary>Always the first day of a month (pay period = calendar month).</summary>
    public DateOnly EffectiveFrom { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal Allowance { get; set; }
    public string Reason { get; set; } = "";

    public static SalaryRate Create(Guid employeeId, DateOnly effectiveFrom, decimal monthlySalary, decimal allowance, string? reason)
    {
        if (effectiveFrom.Day != 1) throw new BusinessException(HrErrors.RateNotFirstOfMonth);
        if (monthlySalary < 0 || allowance < 0) throw new BusinessException(HrErrors.InvalidSalary);
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException(HrErrors.ReasonRequired);
        return new SalaryRate
        {
            EmployeeId = employeeId, EffectiveFrom = effectiveFrom,
            MonthlySalary = Math.Round(monthlySalary, 0, MidpointRounding.AwayFromZero),
            Allowance = Math.Round(allowance, 0, MidpointRounding.AwayFromZero),
            Reason = reason.Trim(),
        };
    }
}
