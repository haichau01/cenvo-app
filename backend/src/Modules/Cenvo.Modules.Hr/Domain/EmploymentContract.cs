namespace Cenvo.Modules.Hr.Domain;

/// <summary>Labour contract; the latest one that has started is the current one.</summary>
public class EmploymentContract : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public ContractType Type { get; set; }
    public DateOnly StartDate { get; set; }
    /// <summary>Null = no end date (indefinite contract).</summary>
    public DateOnly? EndDate { get; set; }
    public string? Note { get; set; }

    public void Update(ContractType type, DateOnly start, DateOnly? end, string? note)
    {
        if (end <= start) throw new BusinessException(HrErrors.ContractEndBeforeStart);
        Type = type;
        StartDate = start;
        EndDate = end;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
