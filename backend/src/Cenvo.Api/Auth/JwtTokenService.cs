using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cenvo.Modules.Organization.Application.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Cenvo.Api.Auth;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "cenvo";
    public string Audience { get; set; } = "cenvo";
    public int AccessMinutes { get; set; } = 60;
}

public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserIdentity User);

public class JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> opt)
{
    public static SymmetricSecurityKey SigningKey(string key) => new(Encoding.UTF8.GetBytes(key));

    public (string token, DateTime expires) Create(UserIdentity u)
    {
        var o = opt.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.AccessMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.UserId.ToString()),
            new(ClaimTypes.Name, u.UserName),
            new(CenvoClaims.FullName, u.FullName),
        };
        if (u.BranchId != null) claims.Add(new Claim(CenvoClaims.Branch, u.BranchId.Value.ToString()));
        claims.AddRange(u.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var jwt = new JwtSecurityToken(o.Issuer, o.Audience, claims, expires: expires,
            signingCredentials: new SigningCredentials(SigningKey(o.Key), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
