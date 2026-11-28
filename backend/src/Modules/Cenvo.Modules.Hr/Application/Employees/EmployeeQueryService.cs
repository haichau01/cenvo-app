namespace Cenvo.Modules.Hr.Application.Employees;

/// <summary>Owner (Admin) only: the whole chain's staff with personal data and pay.</summary>
public class EmployeeQueryService(AppDbContext db, IUserAccounts accounts)
{
    public async Task<List<EmployeeListItemDto>> List(CancellationToken ct)
    {
        var today = VnTime.Today;
        var users = await accounts.List(null, ct);
        var profiles = await db.Set<Employee>().AsNoTracking()
            .Select(e => new EmployeeRow
            {
                Id = e.Id,
                Department = e.Department,
                Position = e.Position,
                HireDate = e.HireDate,
                LeaveDate = e.LeaveDate,
                CurrentContract = e.Contracts.Where(c => c.StartDate <= today).OrderByDescending(c => c.StartDate)
                    .Select(c => new ContractRow { Type = c.Type, EndDate = c.EndDate }).FirstOrDefault(),
                MonthlySalary = e.SalaryRates.Where(r => r.EffectiveFrom <= today).OrderByDescending(r => r.EffectiveFrom)
                    .Select(r => (decimal?)r.MonthlySalary).FirstOrDefault(),
            })
            .ToDictionaryAsync(e => e.Id, ct);

        return users
            .Select(u => ToListItem(u, profiles.GetValueOrDefault(u.Id), today))
            .OrderByDescending(e => e.IsActive).ThenBy(e => e.FullName)
            .ToList();
    }

    public async Task<EmployeeDetailDto> Get(Guid id, CancellationToken ct)
    {
        var account = (await accounts.List([id], ct)).FirstOrDefault() ?? throw new NotFoundException("nhân viên", id);
        var employee = await db.Set<Employee>().AsNoTracking()
            .Include(e => e.Contracts).Include(e => e.SalaryRates).AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee == null) return new EmployeeDetailDto(account, Profile: null, Contracts: [], SalaryRates: [], CurrentRate: null);

        var today = VnTime.Today;
        var rates = employee.SalaryRates.OrderByDescending(r => r.EffectiveFrom).Select(r => ToDto(r, today)).ToList();
        var current = SalaryHistory.EffectiveOn(employee.SalaryRates, today);
        return new EmployeeDetailDto(
            Account: account,
            Profile: ToProfile(employee),
            Contracts: employee.Contracts.OrderByDescending(c => c.StartDate)
                .Select(c => new ContractDto(c.Id, c.Type, c.StartDate, c.EndDate, c.Note)).ToList(),
            SalaryRates: rates,
            CurrentRate: current == null ? null : ToDto(current, today));
    }

    private static EmployeeListItemDto ToListItem(UserAccount u, EmployeeRow? p, DateOnly today) =>
        new(Id: u.Id, UserName: u.UserName, FullName: u.FullName, IsActive: u.IsActive, Roles: u.Roles, BranchIds: u.BranchIds,
            HasProfile: p != null, Department: p?.Department, Position: p?.Position, HireDate: p?.HireDate, LeaveDate: p?.LeaveDate,
            ContractType: p?.CurrentContract?.Type, ContractEnd: p?.CurrentContract?.EndDate,
            ContractEndsSoon: p is { LeaveDate: null } && ContractAlerts.EndsSoon(p.CurrentContract?.EndDate, today),
            MonthlySalary: p?.MonthlySalary);

    private static EmployeeProfileDto ToProfile(Employee e) => new()
    {
        Department = e.Department,
        Position = e.Position,
        HireDate = e.HireDate,
        LeaveDate = e.LeaveDate,
        NationalId = e.NationalId,
        Phone = e.Phone,
        BirthDate = e.BirthDate,
        Address = e.Address,
        Note = e.Note,
    };

    private static SalaryRateDto ToDto(SalaryRate r, DateOnly today) =>
        new(r.Id, r.EffectiveFrom, r.MonthlySalary, r.Allowance, r.Reason, CanRemove: r.EffectiveFrom > today);
}
