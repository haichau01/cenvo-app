namespace Cenvo.Modules.Organization.Domain;

public class Branch : BaseEntity, ISoftDelete
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    /// <summary>Allows negative stock for manual receipts/issues. POS sales may always go negative (with a warning).</summary>
    public bool AllowNegativeStock { get; set; }
    public bool IsDeleted { get; set; }

    public void Update(string code, string name, string? address, string? phone, bool allowNegativeStock)
    {
        Code = code.Trim();
        Name = name.Trim();
        Address = address;
        Phone = phone;
        AllowNegativeStock = allowNegativeStock;
    }
}
