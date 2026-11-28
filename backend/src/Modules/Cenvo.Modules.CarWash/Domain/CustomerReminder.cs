using System.ComponentModel.DataAnnotations;

namespace Cenvo.Modules.CarWash.Domain;

/// <summary>
/// One reminder message for one reason (<see cref="Kind"/> + <see cref="RefId"/>): the latest wash ticket for a return
/// reminder, the membership for package reminders. Unique per reason, so a customer never gets the same reminder twice;
/// a new wash or a new package starts a new reason. Kept as the send log — never deleted.
/// </summary>
public class CustomerReminder : BaseEntity, IBranchScoped
{
    /// <summary>A send that has not finished in this time (process crashed) may be sent again.</summary>
    public static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(10);
    public const int MaxErrorLength = 500;

    public Guid BranchId { get; set; }
    public ReminderKind Kind { get; set; }
    public Guid RefId { get; set; }
    public Guid CustomerId { get; set; }
    /// <summary>Snapshot at send time (the customer may change number later).</summary>
    public string Phone { get; set; } = "";
    public string Message { get; set; } = "";
    public ReminderStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime AttemptedAt { get; set; }
    public Guid? AttemptedBy { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }
    /// <summary>Two managers retrying the same failed reminder at once must not both send it.</summary>
    [Timestamp] public uint Version { get; set; }

    public static CustomerReminder Claim(ReminderKind kind, Guid refId, Guid customerId, string phone, string message,
        Guid? by, DateTime now)
    {
        var reminder = new CustomerReminder { Kind = kind, RefId = refId, CustomerId = customerId };
        reminder.Retry(phone, message, by, now);
        return reminder;
    }

    /// <summary>Whether the reason can be sent (again): never for one already sent or still being sent.</summary>
    public bool CanSend(DateTime now) =>
        Status == ReminderStatus.Failed || (Status == ReminderStatus.Pending && AttemptedAt < now - PendingTimeout);

    /// <summary>Claims a failed (or stuck) reminder for another attempt.</summary>
    public void Retry(string phone, string message, Guid? by, DateTime now)
    {
        if (Attempts > 0 && !CanSend(now)) throw new BusinessException(CarWashErrors.ReminderAlreadySent);
        Phone = phone;
        Message = message;
        Status = ReminderStatus.Pending;
        Attempts++;
        AttemptedAt = now;
        AttemptedBy = by;
    }

    public void MarkSent(string? providerMessageId, DateTime now)
    {
        Status = ReminderStatus.Sent;
        SentAt = now;
        ProviderMessageId = providerMessageId;
        LastError = null;
    }

    public void MarkFailed(string? error)
    {
        Status = ReminderStatus.Failed;
        var text = string.IsNullOrWhiteSpace(error) ? CarWashErrors.ReminderSendFailed : error.Trim();
        LastError = text.Length > MaxErrorLength ? text[..MaxErrorLength] : text;
    }
}
