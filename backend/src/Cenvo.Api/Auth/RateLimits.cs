using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Cenvo.Api.Auth;

/// <summary>Rate limits for endpoints that check a password (login, manager approval) — slows down guessing.</summary>
public static class RateLimits
{
    public const string Credentials = nameof(Credentials);
    private const int DefaultAttemptsPerMinute = 10;
    private const string TooMany = "Thử quá nhiều lần. Đợi một phút rồi thử lại.";

    /// <param name="attemptsPerMinute">From config <c>RateLimits:CredentialAttemptsPerMinute</c> (tests raise it).</param>
    public static IServiceCollection AddCenvoRateLimits(this IServiceCollection services, int? attemptsPerMinute)
    {
        var limit = attemptsPerMinute ?? DefaultAttemptsPerMinute;
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Per signed-in user (a cashier guessing a manager's password), otherwise per IP address (login).
            o.AddPolicy(Credentials, http => RateLimitPartition.GetFixedWindowLimiter(
                http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1) }));
            o.OnRejected = (context, ct) =>
                new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(new { title = TooMany }, ct));
        });
    }
}
