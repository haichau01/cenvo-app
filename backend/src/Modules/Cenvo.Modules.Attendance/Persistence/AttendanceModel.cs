namespace Cenvo.Modules.Attendance.Persistence;

public class AttendanceModel : IModuleModel
{
    public void Build(ModelBuilder mb)
    {
        mb.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(x => new { x.BranchId, x.WorkDate });
            e.HasIndex(x => new { x.EmployeeId, x.CheckInAt }); // the person's open punch and own history, across branches
            // One punch per planned shift (a second scan on the same shift is its check-out).
            e.HasIndex(x => x.ShiftAssignmentId).IsUnique().HasFilter("shift_assignment_id IS NOT NULL");
            e.Property(x => x.EmployeeName).HasMaxLength(100);
            e.Property(x => x.ShiftName).HasMaxLength(50);
            e.Property(x => x.ReviewedBy).HasMaxLength(100);
            e.Property(x => x.ReviewNote).HasMaxLength(300);
            e.HasMany(x => x.Edits).WithOne().HasForeignKey(x => x.AttendanceRecordId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<AttendanceEdit>(e =>
        {
            e.Property(x => x.EditorName).HasMaxLength(100);
            e.Property(x => x.Reason).HasMaxLength(300);
        });
        mb.Entity<Holiday>(e =>
        {
            e.HasIndex(x => x.Date).IsUnique().HasFilter("is_deleted = false");
            e.Property(x => x.Name).HasMaxLength(100);
        });
    }
}
