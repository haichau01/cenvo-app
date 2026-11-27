namespace Cenvo.Modules.Inventory.Persistence;

public class InventoryModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        mb.Entity<Warehouse>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        mb.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
        mb.Entity<StockDocument>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.BranchId, x.Date });
            e.HasIndex(x => new { x.RefType, x.RefId });
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DocumentId);
            e.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Note).HasMaxLength(2000);
            e.Ignore(x => x.IsAwaitingReceipt);
        });
        mb.Entity<StockDocumentLine>(e =>
        {
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.BaseUnitCost);
        });
        mb.Entity<StockMovement>(e =>
        {
            e.HasIndex(x => new { x.WarehouseId, x.ProductId, x.Date });
            e.HasIndex(x => x.DocumentId);
            e.HasIndex(x => new { x.BranchId, x.DocumentType, x.Date });
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<StockBalance>(e =>
        {
            e.HasKey(x => new { x.WarehouseId, x.ProductId });
            e.HasIndex(x => x.BranchId);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
