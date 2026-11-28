namespace Cenvo.Modules.CarWash.Application.Reminders;

/// <summary>One reason to remind one customer, as found in SQL. Fields that do not apply to the kind are null.</summary>
/// <remarks>Built in SQL → init properties.</remarks>
public record DueReminderRow
{
    public required ReminderKind Kind { get; init; }
    /// <summary>Latest wash ticket (return visit) or the membership (package reminders).</summary>
    public required Guid RefId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string Phone { get; init; }
    public string? Plate { get; init; }
    public DateTime? LastWashAt { get; init; }
    public string? PackageName { get; init; }
    public int? RemainingUses { get; init; }
    public DateTime? ExpiresAt { get; init; }
    /// <summary>Error of an earlier failed attempt, so staff see why it is still on the list.</summary>
    public string? LastError { get; init; }
}

/// <summary>A due reminder plus the exact message that would be sent.</summary>
public record DueReminderDto(DueReminderRow Reason, string Message);

public record ReminderSettingsDto(
    int ReturnAfterDays, int ExpiringWithinDays, int LowUsesThreshold,
    string ReturnTemplate, string ExpiringTemplate, string LowUsesTemplate,
    IReadOnlyList<string> Placeholders, int MaxTemplateLength);

public record ReminderSettingsInput(
    int ReturnAfterDays, int ExpiringWithinDays, int LowUsesThreshold,
    string ReturnTemplate, string ExpiringTemplate, string LowUsesTemplate);

public record SendRemindersInput(ReminderKind Kind, List<Guid> RefIds);

public record ReminderFailureDto(Guid RefId, string CustomerName, string Error);

/// <param name="Skipped">Selected but no longer due (already sent, customer opted out, came back, package used…).</param>
public record SendRemindersResult(int Sent, int Skipped, List<ReminderFailureDto> Failures);

public record SetAcceptsRemindersInput(bool Accepts);

/// <remarks>Built in SQL → init properties.</remarks>
public record ReminderLogDto
{
    public required Guid Id { get; init; }
    public required ReminderKind Kind { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string Phone { get; init; }
    public required string Message { get; init; }
    public required ReminderStatus Status { get; init; }
    public required int Attempts { get; init; }
    public required DateTime AttemptedAt { get; init; }
    public required DateTime? SentAt { get; init; }
    public required string? LastError { get; init; }
}
