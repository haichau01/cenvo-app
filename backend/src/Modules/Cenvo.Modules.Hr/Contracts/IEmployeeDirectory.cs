namespace Cenvo.Modules.Hr.Contracts;

/// <summary>Work facts about employees for modules that plan or count work (schedule, attendance, payroll).</summary>
public interface IEmployeeDirectory
{
    /// <summary>Department of each employee that has an HR profile; employees without one are missing from the result.</summary>
    Task<Dictionary<Guid, Department>> Departments(IReadOnlyCollection<Guid> employeeIds, CancellationToken ct);
}
