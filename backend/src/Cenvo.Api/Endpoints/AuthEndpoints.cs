using Cenvo.Modules.Organization.Application.Auth;

namespace Cenvo.Api.Endpoints;

public record LoginRequest(string UserName, string Password, Guid? BranchId);
public record RefreshRequest(string RefreshToken);
public record SwitchBranchRequest(Guid BranchId);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", async (LoginRequest r, AuthService svc, JwtTokenService jwt, CancellationToken ct) =>
            await Issue(await svc.Login(r.UserName.Trim().ToLowerInvariant(), r.Password, r.BranchId, ct), svc, jwt, ct))
            .RequireRateLimiting(RateLimits.Credentials);

        auth.MapPost("/refresh", async (RefreshRequest r, AuthService svc, JwtTokenService jwt, CancellationToken ct) =>
            await Issue(await svc.Refresh(r.RefreshToken, ct), svc, jwt, ct));

        auth.MapPost("/logout", (RefreshRequest r, AuthService svc, CancellationToken ct) => svc.Revoke(r.RefreshToken, ct));

        auth.MapPost("/switch-branch", async (SwitchBranchRequest r, CurrentUser me, AuthService svc, JwtTokenService jwt,
            CancellationToken ct) => await Issue(await svc.SwitchBranch(me.UserId!.Value, r.BranchId, ct), svc, jwt, ct))
            .RequireAuthorization();
    }

    private static async Task<TokenResponse> Issue(UserIdentity identity, AuthService svc, JwtTokenService jwt, CancellationToken ct)
    {
        var (token, expires) = jwt.Create(identity);
        var refresh = await svc.IssueRefreshToken(identity.UserId, identity.BranchId, ct);
        return new TokenResponse(token, refresh, expires, identity);
    }
}
