namespace Cenvo.Modules.CarWash.Application.Customers;

/// <summary>Finds the customer for a phone number typed at the counter, creating one the first time.</summary>
internal static class CustomerResolution
{
    /// <returns>The tracked customer, or null when no phone number was given.</returns>
    public static async Task<Customer?> FindOrCreateByPhone(this AppDbContext db, string? phone, string? name, CancellationToken ct)
    {
        var normalized = PhoneNumber.Normalize(phone);
        if (normalized == null) return null;

        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Phone == normalized, ct);
        if (customer == null) return db.Add(Customer.Create(normalized, name)).Entity;
        customer.RenameIfGiven(name);
        return customer;
    }
}
