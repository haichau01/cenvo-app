namespace Cenvo.Modules.Hr.Application.Employees;

/// <summary>Account + profile in one form: one employee = one login account.</summary>
/// <param name="Password">Required for a new employee; empty = keep the current password.</param>
public record EmployeeInput(
    string UserName, string FullName, string? Password, string[] Roles, Guid[] BranchIds,
    Department Department, string Position, DateOnly HireDate,
    string? NationalId, string? Phone, DateOnly? BirthDate, string? Address, string? Note);

public record LeaveInput(DateOnly LeaveDate);

public record ContractInput(ContractType Type, DateOnly StartDate, DateOnly? EndDate, string? Note);

public record SalaryRateInput(DateOnly EffectiveFrom, decimal MonthlySalary, decimal Allowance, string Reason, decimal OvertimeHourlyRate = 0);

/// <summary>Current contract and pay of a profile, as of today.</summary>
/// <remarks>Built in SQL → init properties.</remarks>
public record EmployeeRow
{
    public required Guid Id { get; init; }
    public required Department Department { get; init; }
    public required string Position { get; init; }
    public required DateOnly HireDate { get; init; }
    public required DateOnly? LeaveDate { get; init; }
    public required ContractRow? CurrentContract { get; init; }
    public required decimal? MonthlySalary { get; init; }
}

/// <summary>The contract in force (latest one that has started).</summary>
/// <remarks>Built in SQL → init properties.</remarks>
public record ContractRow
{
    public required ContractType Type { get; init; }
    public required DateOnly? EndDate { get; init; }
}

/// <summary>A login account with its HR profile, if one was filled in (seeded accounts may not have one yet).</summary>
public record EmployeeListItemDto(
    Guid Id, string UserName, string FullName, bool IsActive, string[] Roles, Guid[] BranchIds, bool HasProfile,
    Department? Department, string? Position, DateOnly? HireDate, DateOnly? LeaveDate,
    ContractType? ContractType, DateOnly? ContractEnd, bool ContractEndsSoon, decimal? MonthlySalary);

/// <remarks>Built in SQL → init properties.</remarks>
public record EmployeeProfileDto
{
    public required Department Department { get; init; }
    public required string Position { get; init; }
    public required DateOnly HireDate { get; init; }
    public required DateOnly? LeaveDate { get; init; }
    public required string? NationalId { get; init; }
    public required string? Phone { get; init; }
    public required DateOnly? BirthDate { get; init; }
    public required string? Address { get; init; }
    public required string? Note { get; init; }
}

public record ContractDto(Guid Id, ContractType Type, DateOnly StartDate, DateOnly? EndDate, string? Note);

public record SalaryRateDto(Guid Id, DateOnly EffectiveFrom, decimal MonthlySalary, decimal Allowance, decimal OvertimeHourlyRate, string Reason,
    bool CanRemove);

public record EmployeeDetailDto(
    UserAccount Account, EmployeeProfileDto? Profile, List<ContractDto> Contracts, List<SalaryRateDto> SalaryRates,
    SalaryRateDto? CurrentRate);
