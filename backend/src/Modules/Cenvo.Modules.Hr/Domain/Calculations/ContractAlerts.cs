namespace Cenvo.Modules.Hr.Domain.Calculations;

public static class ContractAlerts
{
    /// <summary>Warn this many days before a contract ends.</summary>
    public const int WarnDays = 30;

    /// <summary>Contract ends within <see cref="WarnDays"/> days (or has already ended) — time to renew.</summary>
    public static bool EndsSoon(DateOnly? endDate, DateOnly today) => endDate != null && endDate.Value <= today.AddDays(WarnDays);
}
