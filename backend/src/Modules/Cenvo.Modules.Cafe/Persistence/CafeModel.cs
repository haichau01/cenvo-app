namespace Cenvo.Modules.Cafe.Persistence;

public class CafeModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        mb.Entity<Area>().HasIndex(x => x.BranchId);
        mb.Entity<DiningTable>(e =>
        {
            e.HasIndex(x => x.BranchId);
            e.HasOne<Area>().WithMany().HasForeignKey(x => x.AreaId);
        });
        mb.Entity<CafeItem>(e =>
        {
            e.HasKey(x => x.ProductId);
            e.HasOne<Product>().WithOne().HasForeignKey<CafeItem>(x => x.ProductId);
        });
        mb.Entity<RecipeLine>(e =>
        {
            e.HasIndex(x => x.MenuProductId);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.MenuProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.IngredientProductId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<Shift>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.BranchId, x.OpenedAt });
            e.HasIndex(x => new { x.CashierId, x.Status });
        });
        mb.Entity<CafeOrder>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.BranchId, x.Status });
            e.HasIndex(x => new { x.BranchId, x.PaidAt });
            e.HasIndex(x => new { x.BranchId, x.CreatedAt });
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
            e.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.OrderId);
            e.HasOne<DiningTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<CafeOrderLine>(e =>
        {
            e.HasMany(x => x.Toppings).WithOne().HasForeignKey(x => x.LineId);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.IsBeingPrepared);
        });
        mb.Entity<CafeOrderLineTopping>()
            .HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<CafePayment>();
    }
}
