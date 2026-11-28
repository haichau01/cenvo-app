using System.Security.Cryptography;
using System.Text;
using Cenvo.Modules.Attendance.Contracts;

namespace Cenvo.Api.Auth;

/// <summary>Signs punch QR codes with a key derived from the JWT key, so no extra secret has to be deployed.</summary>
public sealed class HmacPunchCodeSigner(string jwtKey) : IPunchCodeSigner
{
    private readonly byte[] _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwtKey), "cenvo-punch-code"u8);

    public byte[] Sign(string payload) => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload));
}
