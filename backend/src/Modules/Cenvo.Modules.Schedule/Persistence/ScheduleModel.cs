namespace Cenvo.Modules.Schedule.Persistence;

public class ScheduleModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        mb.Entity<ShiftTemplate>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.SortOrder });
            e.Property(x => x.Name).HasMaxLength(50);
            e.HasMany(x => x.Requirements).WithOne().HasForeignKey(x => x.ShiftTemplateId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<ShiftRequirement>().HasKey(x => new { x.ShiftTemplateId, x.Department });
        mb.Entity<ShiftAssignment>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.Date });
            e.HasIndex(x => new { x.EmployeeId, x.StartsAt }); // overlap and weekly hours, across branches
            e.HasIndex(x => new { x.EmployeeId, x.Date, x.ShiftTemplateId }).IsUnique();
            e.Property(x => x.EmployeeName).HasMaxLength(100);
            e.Property(x => x.ShiftName).HasMaxLength(50);
            e.Ignore(x => x.Window);
        });
        mb.Entity<ScheduleSettings>().HasIndex(x => x.BranchId).IsUnique();
    }
}
