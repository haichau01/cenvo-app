using Cenvo.Modules.CarWash.Contracts;

namespace Cenvo.Api.Messaging;

/// <summary>
/// Stand-in until a Zalo OA / SMS account exists: "sends" by writing a log line and always succeeds.
/// Replace the registration in Program.cs with the real provider — nothing else changes.
/// </summary>
public class LogReminderSender(ILogger<LogReminderSender> log) : IReminderSender
{
    public const string ProviderPrefix = "log-";

    public Task<ReminderSendResult> Send(string phone, string message, CancellationToken ct)
    {
        var id = ProviderPrefix + Guid.NewGuid().ToString("N")[..12];
        // Phone masked: logs are read by more people than the customer list.
        log.LogInformation("Reminder {MessageId} to {Phone} ({Length} chars) — not sent, no provider configured",
            id, Mask(phone), message.Length);
        return Task.FromResult(ReminderSendResult.Sent(id));
    }

    private static string Mask(string phone) => phone.Length <= 3 ? "***" : new string('*', phone.Length - 3) + phone[^3..];
}
