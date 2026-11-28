namespace Cenvo.Modules.Hr.Domain;

/// <summary>
/// HR profile of one login account: <see cref="BaseEntity.Id"/> is the account (AppUser) id — one employee = one account.
/// Holds personal data (national ID, phone, address) that only the owner (Admin) may read.
/// </summary>
public class Employee : BaseEntity
{
    public Department Department { get; set; }
    public string Position { get; set; } = "";
    public DateOnly HireDate { get; set; }
    public DateOnly? LeaveDate { get; set; }
    /// <summary>CCCD, 12 digits.</summary>
    public string? NationalId { get; set; }
    /// <summary>Digits only.</summary>
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public List<EmploymentContract> Contracts { get; set; } = [];
    public List<SalaryRate> SalaryRates { get; set; } = [];

    public bool HasLeft => LeaveDate != null;

    public static Employee For(Guid userId) => new() { Id = userId };

    public void Update(Department department, string? position, DateOnly hireDate, string? nationalId, string? phone,
        DateOnly? birthDate, string? address, string? note)
    {
        if (string.IsNullOrWhiteSpace(position)) throw new BusinessException(HrErrors.PositionRequired);
        if (LeaveDate < hireDate) throw new BusinessException(HrErrors.LeaveBeforeHire);
        Department = department;
        Position = position.Trim();
        HireDate = hireDate;
        NationalId = PersonalData.NationalId(nationalId);
        Phone = PersonalData.Phone(phone);
        BirthDate = birthDate;
        Address = Clean(address);
        Note = Clean(note);
    }

    /// <summary>Stops employment; the caller also locks the login account.</summary>
    public void Leave(DateOnly leaveDate)
    {
        if (HasLeft) throw new BusinessException(HrErrors.AlreadyLeft);
        if (leaveDate < HireDate) throw new BusinessException(HrErrors.LeaveBeforeHire);
        LeaveDate = leaveDate;
    }

    public void Rehire()
    {
        if (!HasLeft) throw new BusinessException(HrErrors.NotLeft);
        LeaveDate = null;
    }

    public EmploymentContract AddContract(ContractType type, DateOnly start, DateOnly? end, string? note)
    {
        var contract = new EmploymentContract { EmployeeId = Id };
        contract.Update(type, start, end, note);
        Contracts.Add(contract);
        return contract;
    }

    /// <summary>A pay change is a new row from the first day of a month; existing rows are never edited.</summary>
    public SalaryRate AddSalaryRate(DateOnly effectiveFrom, decimal monthlySalary, decimal allowance, string? reason)
    {
        if (SalaryRates.Any(r => r.EffectiveFrom == effectiveFrom)) throw new BusinessException(HrErrors.RateDateTaken);
        var rate = SalaryRate.Create(Id, effectiveFrom, monthlySalary, allowance, reason);
        SalaryRates.Add(rate);
        return rate;
    }

    /// <summary>Only a rate that has not started yet can be removed (typed by mistake).</summary>
    public SalaryRate RemoveSalaryRate(Guid rateId, DateOnly today)
    {
        var rate = SalaryRates.FirstOrDefault(r => r.Id == rateId) ?? throw new NotFoundException("mức lương", rateId);
        if (rate.EffectiveFrom <= today) throw new BusinessException(HrErrors.RateAlreadyApplied);
        SalaryRates.Remove(rate);
        return rate;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
