namespace Cenvo.Modules.CarWash.Persistence;

public class CarWashModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        BuildCatalog(mb);
        BuildCustomers(mb);
        BuildTickets(mb);
    }

    private static void BuildCatalog(ModelBuilder mb)
    {
        mb.Entity<WashOffering>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted = false");
            e.HasMany(x => x.Prices).WithOne().HasForeignKey(x => x.OfferingId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Supplies).WithOne().HasForeignKey(x => x.OfferingId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<WashOfferingPrice>().HasIndex(x => new { x.OfferingId, x.VehicleType }).IsUnique();
        mb.Entity<WashOfferingSupply>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<MembershipPackage>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted = false");
            e.HasOne<WashOffering>().WithMany().HasForeignKey(x => x.OfferingId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void BuildCustomers(ModelBuilder mb)
    {
        mb.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.Phone).IsUnique().HasFilter("is_deleted = false");
            e.HasIndex(x => x.Name);
            e.HasIndex(x => new { x.LastWashBranchId, x.LastWashAt }); // return reminders
        });
        mb.Entity<ReminderSettings>().HasIndex(x => x.BranchId).IsUnique();
        mb.Entity<CustomerReminder>(e =>
        {
            e.HasIndex(x => new { x.Kind, x.RefId }).IsUnique(); // one reminder per reason, ever
            e.HasIndex(x => new { x.BranchId, x.AttemptedAt });
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Message).HasMaxLength(ReminderTemplate.MaxMessageLength);
            e.Property(x => x.LastError).HasMaxLength(CustomerReminder.MaxErrorLength);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<Vehicle>(e =>
        {
            e.HasIndex(x => x.Plate).IsUnique().HasFilter("is_deleted = false");
            e.HasIndex(x => x.CustomerId);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });
        mb.Entity<CustomerMembership>(e =>
        {
            e.HasIndex(x => new { x.SoldBranchId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.CustomerId, x.Status });
            e.HasIndex(x => new { x.SoldBranchId, x.PaidAt }); // sales report
            e.HasIndex(x => x.ShiftId);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MembershipPackage>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void BuildTickets(ModelBuilder mb)
    {
        mb.Entity<WashTicket>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.BranchId, x.Status });
            e.HasIndex(x => new { x.BranchId, x.CreatedAt });
            e.HasIndex(x => new { x.BranchId, x.CompletedAt });
            e.HasIndex(x => new { x.BranchId, x.PaidAt }); // sales report
            e.HasIndex(x => new { x.BranchId, x.VehicleId });
            e.HasIndex(x => x.ShiftId);
            e.Ignore(x => x.IsPaid);
            e.Ignore(x => x.IsEditable);
            e.Ignore(x => x.CanAssignStaff);
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Staff).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Photos).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<WashTicketLine>(e =>
        {
            e.HasOne<WashOffering>().WithMany().HasForeignKey(x => x.OfferingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CustomerMembership>().WithMany().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<WashTicketStaff>().HasIndex(x => x.UserId);
        mb.Entity<WashPayment>();
        mb.Entity<WashTicketPhoto>();
    }
}
