namespace Cenvo.Modules.Organization.Domain;

/// <summary>Only the refresh token hash is stored; each token is single-use (rotation).</summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public Guid? BranchId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsUsable(DateTime now) => RevokedAt == null && ExpiresAt > now;
    public void Revoke(DateTime now) => RevokedAt ??= now;
}
