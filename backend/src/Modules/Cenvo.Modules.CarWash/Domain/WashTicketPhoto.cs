namespace Cenvo.Modules.CarWash.Domain;

/// <summary>Photo of the vehicle's condition at intake (avoids scratch disputes). The file lives in photo storage.</summary>
public class WashTicketPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    /// <summary>Opaque key returned by the storage (never a client-supplied path).</summary>
    public string StorageKey { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime TakenAt { get; set; }
    public Guid? TakenBy { get; set; }
}
