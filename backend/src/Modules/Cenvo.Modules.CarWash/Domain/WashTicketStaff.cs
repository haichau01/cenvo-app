namespace Cenvo.Modules.CarWash.Domain;

/// <summary>A washer assigned to the ticket. Commission is set when the wash is completed.</summary>
public class WashTicketStaff
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public decimal Commission { get; set; }
}
