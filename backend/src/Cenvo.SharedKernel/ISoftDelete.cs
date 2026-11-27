namespace Cenvo.SharedKernel;

/// <summary>Soft-deleted catalog data: <c>db.Remove</c> only sets IsDeleted.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
