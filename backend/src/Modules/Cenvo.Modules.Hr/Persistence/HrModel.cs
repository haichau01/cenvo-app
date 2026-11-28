namespace Cenvo.Modules.Hr.Persistence;

/// <param name="cipher">National ID and phone are stored encrypted (only the owner may read them).</param>
public class HrModel(IPersonalDataCipher cipher) : IModuleModel
{
    public const int MaxPhoneLength = 15;

    public void Build(ModelBuilder mb)
    {
        // Employee.Id = AppUser.Id (one employee = one account); kept consistent by EmployeeService, no FK across modules.
        mb.Entity<Employee>(e =>
        {
            e.Ignore(x => x.HasLeft);
            e.Property(x => x.Position).HasMaxLength(100);
            e.Property(x => x.NationalId).IsEncrypted(cipher, PersonalData.NationalIdLength);
            e.Property(x => x.Phone).IsEncrypted(cipher, MaxPhoneLength);
            e.HasMany(x => x.Contracts).WithOne().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.SalaryRates).WithOne().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<EmploymentContract>().HasIndex(x => new { x.EmployeeId, x.StartDate });
        mb.Entity<SalaryRate>().HasIndex(x => new { x.EmployeeId, x.EffectiveFrom }).IsUnique();
        mb.Entity<PayAdjustment>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.Date });
            e.HasIndex(x => new { x.EmployeeId, x.Date }); // payroll per person per month
        });
    }
}
