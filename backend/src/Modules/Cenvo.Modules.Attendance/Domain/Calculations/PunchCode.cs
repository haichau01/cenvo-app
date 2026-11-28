using System.Security.Cryptography;
using System.Text;

namespace Cenvo.Modules.Attendance.Domain.Calculations;

/// <summary>Where a punch code was shown: the branch, and the account signed in on that screen.</summary>
public readonly record struct PunchOrigin(Guid BranchId, Guid ShownBy);

/// <summary>
/// The rotating code the shop POS shows as a QR: branch + the account showing it + 30-second window, signed. A photo of it
/// sent to someone at home stops working within a minute, so people punch only while standing in the shop.
/// </summary>
public static class PunchCode
{
    public static readonly TimeSpan Step = TimeSpan.FromSeconds(30);
    private const int SignatureBytes = 16;

    public static string Issue(PunchOrigin origin, DateTime now, IPunchCodeSigner signer) => Encode(origin, Window(now), signer);

    /// <summary>When the code shown now stops being accepted (it is also accepted during the following window).</summary>
    public static DateTime ExpiresAt(DateTime now) => new DateTime((Window(now) + 2) * Step.Ticks, DateTimeKind.Utc);

    /// <summary>Where the code was shown, or null when it is forged, malformed or older than one window.</summary>
    public static PunchOrigin? Read(string? code, DateTime now, IPunchCodeSigner signer)
    {
        var parts = (code ?? "").Split('.');
        if (parts.Length != 4 || !Guid.TryParse(parts[0], out var branchId) || !Guid.TryParse(parts[1], out var shownBy)
            || !long.TryParse(parts[2], out var window)) return null;

        var current = Window(now);
        if (window != current && window != current - 1) return null;
        var origin = new PunchOrigin(branchId, shownBy);
        var expected = Encode(origin, window, signer);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(code!)) ? origin : null;
    }

    private static long Window(DateTime now) => now.Ticks / Step.Ticks;

    private static string Encode(PunchOrigin origin, long window, IPunchCodeSigner signer)
    {
        var payload = $"{origin.BranchId:N}.{origin.ShownBy:N}.{window}";
        var signature = signer.Sign(payload)[..SignatureBytes];
        return $"{payload}.{Convert.ToHexString(signature)}";
    }
}
