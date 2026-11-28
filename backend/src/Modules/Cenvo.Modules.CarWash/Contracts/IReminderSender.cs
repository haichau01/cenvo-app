namespace Cenvo.Modules.CarWash.Contracts;

/// <param name="ProviderMessageId">Id the provider gives the message (for support / delivery reports).</param>
/// <param name="Error">Why the provider refused, in words staff can act on.</param>
public record ReminderSendResult(bool Succeeded, string? ProviderMessageId = null, string? Error = null)
{
    public static ReminderSendResult Sent(string? providerMessageId) => new(true, providerMessageId);
    public static ReminderSendResult Failed(string error) => new(false, Error: error);
}

/// <summary>
/// Sends one reminder text to a customer's phone (Zalo OA / SMS). The Api implements it; until a provider account
/// exists, the implementation only writes a log line. Must not throw for a refused message — return <see cref="ReminderSendResult.Failed"/>.
/// </summary>
public interface IReminderSender
{
    /// <param name="phone">Digits only, as stored (e.g. 0901234567).</param>
    Task<ReminderSendResult> Send(string phone, string message, CancellationToken ct);
}
