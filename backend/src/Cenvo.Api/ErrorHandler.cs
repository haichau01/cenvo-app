using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Api;

/// <summary>Maps exceptions to ProblemDetails carrying a Vietnamese, user-facing message.</summary>
public class ErrorHandler(ILogger<ErrorHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            BusinessException => (StatusCodes.Status400BadRequest, ex.Message),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Dữ liệu gửi lên không hợp lệ."),
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Dữ liệu vừa được máy khác cập nhật. Tải lại rồi thử lại."),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống, vui lòng thử lại."),
        };
        if (status == StatusCodes.Status500InternalServerError) log.LogError(ex, "Unhandled exception");

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title }, ct);
        return true;
    }
}
