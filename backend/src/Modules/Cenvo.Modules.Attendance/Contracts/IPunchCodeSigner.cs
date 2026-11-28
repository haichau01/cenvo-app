namespace Cenvo.Modules.Attendance.Contracts;

/// <summary>Signs punch codes with a server secret (the Api derives it from its own key; it never leaves the server).</summary>
public interface IPunchCodeSigner
{
    /// <summary>HMAC of the payload, at least 16 bytes.</summary>
    byte[] Sign(string payload);
}
