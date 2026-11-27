namespace Cenvo.Modules.Organization.Domain;

/// <summary>Branch the user is allowed to work in.</summary>
public class UserBranch
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
}
