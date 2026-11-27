namespace Cenvo.Infrastructure.Persistence;

/// <summary>Document number counter per (branch, prefix + date).</summary>
public class DocumentSequence
{
    public Guid BranchId { get; set; }
    public string Key { get; set; } = "";
    public int LastNumber { get; set; }
}
