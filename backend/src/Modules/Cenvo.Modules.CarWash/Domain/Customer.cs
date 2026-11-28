namespace Cenvo.Modules.CarWash.Domain;

/// <summary>
/// Car wash customer, identified by phone number — shared by every branch (the same customer may visit several branches).
/// Without a phone number nothing is saved: the ticket only keeps the name the staff typed.
/// </summary>
public class Customer : BaseEntity, ISoftDelete
{
    /// <summary>Digits only (see <see cref="PhoneNumber"/>); unique among customers that are not deleted.</summary>
    public string Phone { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Note { get; set; }
    /// <summary>Customer agrees to receive reminder messages. On by default; staff turn it off when the customer asks.</summary>
    public bool AcceptsReminders { get; set; } = true;
    /// <summary>Latest finished wash anywhere in the chain — the return reminder counts days from it.</summary>
    public DateTime? LastWashAt { get; set; }
    /// <summary>Branch of the latest wash: that branch sends the return reminder.</summary>
    public Guid? LastWashBranchId { get; set; }
    public Guid? LastTicketId { get; set; }
    public bool IsDeleted { get; set; }

    /// <param name="phone">Already normalized.</param>
    public static Customer Create(string phone, string? name)
    {
        var customer = new Customer { Phone = phone };
        customer.Update(name, phone, null);
        return customer;
    }

    /// <param name="phone">Already normalized.</param>
    public void Update(string? name, string phone, string? note)
    {
        if (string.IsNullOrWhiteSpace(phone)) throw new BusinessException(CarWashErrors.CustomerPhoneRequired);
        Phone = phone;
        Name = string.IsNullOrWhiteSpace(name) ? phone : name.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>Staff typed a name at intake for a known customer → keep the latest name.</summary>
    public void RenameIfGiven(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
    }

    /// <summary>A wash finished → remember it as the latest visit unless a later one is already known.</summary>
    public void RecordWash(Guid ticketId, Guid branchId, DateTime completedAt)
    {
        if (LastWashAt != null && LastWashAt > completedAt) return;
        SetLastWash(ticketId, branchId, completedAt);
    }

    /// <summary>Replaces the latest visit (recomputed after that ticket was cancelled); all null = no visit left.</summary>
    public void SetLastWash(Guid? ticketId, Guid? branchId, DateTime? completedAt)
    {
        LastTicketId = ticketId;
        LastWashBranchId = branchId;
        LastWashAt = completedAt;
    }
}
