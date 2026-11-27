namespace Cenvo.SharedKernel;

/// <summary>Entity owned by a branch — filtered by a global query filter; BranchId is set automatically on insert.</summary>
public interface IBranchScoped
{
    Guid BranchId { get; set; }
}
