using Microsoft.AspNetCore.Identity;

namespace Cenvo.Modules.Organization.Domain;

public class AppUser : BaseEntity, ISoftDelete
{
    private static readonly PasswordHasher<AppUser> Hasher = new();

    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public List<UserRole> Roles { get; set; } = [];
    public List<UserBranch> Branches { get; set; } = [];
    /// <summary>Hashed 4–6 digit PIN for punching in on the shop POS when the phone cannot scan; null = not set.</summary>
    public string? PinHash { get; set; }
    public int PinFailedCount { get; set; }
    public DateTime? PinLockedUntil { get; set; }

    public const int MinPasswordLength = 6;

    public string[] RoleNames => Roles.Select(r => r.Role).ToArray();

    /// <summary>Lower case, 3–50 of a–z, 0–9, dot, dash, underscore (typed on a phone keyboard at the counter).</summary>
    public static string NormalizeUserName(string? userName)
    {
        var normalized = (userName ?? "").Trim().ToLowerInvariant();
        if (normalized.Length is < 3 or > 50 || !normalized.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '-' or '_'))
            throw new BusinessException(OrganizationErrors.InvalidUserName);
        return normalized;
    }

    /// <param name="userName">Already normalized.</param>
    public void UpdateAccount(string userName, string? fullName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new BusinessException(OrganizationErrors.FullNameRequired);
        UserName = userName;
        FullName = fullName.Trim();
        IsActive = isActive;
    }

    public void SetPassword(string password)
    {
        if (password.Length < MinPasswordLength) throw new BusinessException(OrganizationErrors.PasswordTooShort);
        PasswordHash = Hasher.HashPassword(this, password);
    }

    public bool VerifyPassword(string password) =>
        Hasher.VerifyHashedPassword(this, PasswordHash, password) != PasswordVerificationResult.Failed;

    public const int MaxPinAttempts = 5;
    public static readonly TimeSpan PinLockout = TimeSpan.FromMinutes(15);

    public void SetPin(string? pin)
    {
        if (pin is not { Length: >= 4 and <= 6 } || !pin.All(char.IsAsciiDigit)) throw new BusinessException(OrganizationErrors.InvalidPin);
        PinHash = Hasher.HashPassword(this, pin);
        PinFailedCount = 0;
        PinLockedUntil = null;
    }

    /// <summary>Five wrong PINs in a row lock PIN punching for 15 minutes (a PIN has few combinations).</summary>
    public bool VerifyPin(string pin, DateTime now)
    {
        if (PinHash == null) throw new BusinessException(OrganizationErrors.PinNotSet);
        if (PinLockedUntil > now) throw new BusinessException(OrganizationErrors.PinLocked);
        if (Hasher.VerifyHashedPassword(this, PinHash, pin) != PasswordVerificationResult.Failed)
        {
            PinFailedCount = 0;
            return true;
        }
        if (++PinFailedCount >= MaxPinAttempts)
        {
            PinFailedCount = 0;
            PinLockedUntil = now.Add(PinLockout);
        }
        return false;
    }

    public void SetRoles(IEnumerable<string> roles)
    {
        var wanted = roles.Distinct().ToList();
        if (wanted.Except(SharedKernel.Roles.All).Any()) throw new BusinessException(OrganizationErrors.InvalidRole);
        Roles.RemoveAll(r => !wanted.Contains(r.Role));
        Roles.AddRange(wanted.Where(r => Roles.All(x => x.Role != r)).Select(r => new UserRole { UserId = Id, Role = r }));
    }

    public void SetBranches(IEnumerable<Guid> branchIds)
    {
        var wanted = branchIds.Distinct().ToList();
        Branches.RemoveAll(b => !wanted.Contains(b.BranchId));
        Branches.AddRange(wanted.Where(b => Branches.All(x => x.BranchId != b)).Select(b => new UserBranch { UserId = Id, BranchId = b }));
    }
}
