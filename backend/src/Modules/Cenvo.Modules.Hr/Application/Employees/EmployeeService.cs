namespace Cenvo.Modules.Hr.Application.Employees;

/// <summary>
/// Owner (Admin) only: hire, edit and let go of employees. The login account and the HR profile are saved together
/// (one transaction), so an employee never exists without an account or the other way round half-saved.
/// </summary>
public class EmployeeService(AppDbContext db, ICurrentUser user, IUserAccounts accounts)
{
    public async Task<Guid> Save(Guid? id, EmployeeInput input, CancellationToken ct)
    {
        var employee = id == null ? null : await Find(id.Value, ct);
        if (id == user.UserId && !input.Roles.Contains(Roles.Admin)) throw new BusinessException(HrErrors.CannotDropOwnAdmin);

        var isActive = employee is not { HasLeft: true };
        var userId = await accounts.Stage(id, new UserAccountInput(
            input.UserName, input.FullName, input.Password, input.Roles, input.BranchIds, isActive), ct);

        employee ??= db.Add(Employee.For(userId)).Entity;
        employee.Update(input.Department, input.Position, input.HireDate, input.NationalId, input.Phone,
            input.BirthDate, input.Address, input.Note);
        await db.SaveChangesAsync(ct);
        return userId;
    }

    /// <summary>Last working day recorded; the account is locked so they can no longer sign in.</summary>
    public async Task Leave(Guid id, LeaveInput input, CancellationToken ct)
    {
        if (id == user.UserId) throw new BusinessException(HrErrors.CannotLeaveSelf);
        var employee = await Find(id, ct) ?? throw new BusinessException(HrErrors.ProfileRequired);
        employee.Leave(input.LeaveDate);
        await accounts.SetActive(id, false, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task Rehire(Guid id, CancellationToken ct)
    {
        var employee = await Find(id, ct) ?? throw new BusinessException(HrErrors.ProfileRequired);
        employee.Rehire();
        await accounts.SetActive(id, true, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<Guid> AddContract(Guid id, ContractInput input, CancellationToken ct) =>
        Change(id, e => e.AddContract(input.Type, input.StartDate, input.EndDate, input.Note).Id, ct);

    public Task<Guid> UpdateContract(Guid id, Guid contractId, ContractInput input, CancellationToken ct) =>
        Change(id, e =>
        {
            var contract = e.Contracts.FirstOrDefault(c => c.Id == contractId) ?? throw new NotFoundException("hợp đồng", contractId);
            contract.Update(input.Type, input.StartDate, input.EndDate, input.Note);
            return contract.Id;
        }, ct);

    public Task<Guid> AddSalaryRate(Guid id, SalaryRateInput input, CancellationToken ct) =>
        Change(id, e => e.AddSalaryRate(input.EffectiveFrom, input.MonthlySalary, input.Allowance, input.Reason).Id, ct);

    public Task<Guid> RemoveSalaryRate(Guid id, Guid rateId, CancellationToken ct) =>
        Change(id, e => e.RemoveSalaryRate(rateId, VnTime.Today).Id, ct);

    private async Task<Guid> Change(Guid id, Func<Employee, Guid> change, CancellationToken ct)
    {
        var employee = await Find(id, ct) ?? throw new BusinessException(HrErrors.ProfileRequired);
        var result = change(employee);
        await db.SaveChangesAsync(ct);
        return result;
    }

    private Task<Employee?> Find(Guid id, CancellationToken ct) =>
        db.Set<Employee>().Include(e => e.Contracts).Include(e => e.SalaryRates).AsSplitQuery().FirstOrDefaultAsync(e => e.Id == id, ct);
}
