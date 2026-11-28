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
}
