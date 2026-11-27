namespace Cenvo.Modules.Catalog.Persistence;

public class CatalogModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        mb.Entity<Category>();
        mb.Entity<Unit>().HasIndex(x => x.Name).IsUnique();
        mb.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.BaseUnit).WithMany().HasForeignKey(x => x.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Units).WithOne().HasForeignKey(x => x.ProductId);
        });
        mb.Entity<ProductUnit>(e =>
        {
            e.HasKey(x => new { x.ProductId, x.UnitId });
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<ProductPrice>().HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique();
    }
}
