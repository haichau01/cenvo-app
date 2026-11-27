namespace Cenvo.Modules.Organization.Persistence;

public class OrganizationModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        mb.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        mb.Entity<AppUser>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserId);
            e.HasMany(x => x.Branches).WithOne().HasForeignKey(x => x.UserId);
            e.Ignore(x => x.RoleNames);
        });
        mb.Entity<UserRole>().HasKey(x => new { x.UserId, x.Role });
        mb.Entity<UserBranch>().HasKey(x => new { x.UserId, x.BranchId });
        mb.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
    }
}
