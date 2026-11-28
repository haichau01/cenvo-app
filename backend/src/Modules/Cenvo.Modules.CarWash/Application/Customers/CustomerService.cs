namespace Cenvo.Modules.CarWash.Application.Customers;

public class CustomerService(AppDbContext db)
{
    public async Task Update(Guid id, CustomerInput input, CancellationToken ct)
    {
        var phone = PhoneNumber.Normalize(input.Phone) ?? throw new BusinessException(CarWashErrors.CustomerPhoneRequired);
        if (await db.Set<Customer>().AnyAsync(c => c.Phone == phone && c.Id != id, ct))
            throw new BusinessException(CarWashErrors.PhoneTaken);

        var customer = await db.Set<Customer>().FindAsync([id], ct) ?? throw new NotFoundException("khách hàng", id);
        customer.Update(input.Name, phone, input.Note);
        await db.SaveChangesAsync(ct);
    }
}
