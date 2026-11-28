using Cenvo.Modules.Attendance.Application.Holidays;
using Cenvo.Modules.Attendance.Application.Punching;
using Cenvo.Modules.Attendance.Application.Review;

namespace Cenvo.Api.Endpoints;

public static class AttendanceEndpoints
{
    public static void MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        // Everyone: punch with their own phone, see their own month.
        var me = app.MapGroup("/api/attendance").WithTags("Chấm công").RequireAuthorization();
        me.MapPost("/punch", (PunchByCodeInput i, PunchService svc, CancellationToken ct) => svc.ByCode(i, ct));
        me.MapGet("/mine", (DateOnly month, TimesheetQueryService svc, CancellationToken ct) => svc.Mine(month, ct));
        me.MapGet("/holidays", (int year, HolidayService svc, CancellationToken ct) => svc.OfYear(year, ct));

        // The shop POS (signed in as the cashier / manager of the branch) shows the QR code and takes PINs.
        var kiosk = app.MapGroup("/api/attendance/kiosk").WithTags("Chấm công — máy ở quán").RequireAuthorization(Policies.Cashiers);
        kiosk.MapGet("/code", (PunchService svc) => svc.KioskCode());
        kiosk.MapGet("/staff", (PunchService svc, CancellationToken ct) => svc.KioskStaff(ct));
        kiosk.MapPost("/pin", (PunchByPinInput i, PunchService svc, CancellationToken ct) => svc.ByPin(i, ct))
            .RequireRateLimiting(RateLimits.Credentials);

        var review = app.MapGroup("/api/attendance").WithTags("Chấm công — quản lý").RequireAuthorization(Policies.Managers);
        review.MapGet("/day", (DateOnly date, AttendanceQueryService svc, CancellationToken ct) => svc.Day(date, ct));
        review.MapGet("/to-review", (AttendanceQueryService svc, CancellationToken ct) => svc.ToReview(ct));
        review.MapGet("/timesheet", (DateOnly month, TimesheetQueryService svc, CancellationToken ct) => svc.Branch(month, ct));
        review.MapGet("/records/{id:guid}/edits", (Guid id, AttendanceQueryService svc, CancellationToken ct) => svc.Edits(id, ct));
        review.MapPost("/records", (ManualInput i, ReviewService svc, CancellationToken ct) => svc.AddManual(i, ct));
        review.MapPut("/records/{id:guid}/times", (Guid id, CorrectInput i, ReviewService svc, CancellationToken ct) => svc.Correct(id, i, ct));
        review.MapPost("/records/{id:guid}/approve", (Guid id, ReviewService svc, CancellationToken ct) => svc.Approve(id, ct));
        review.MapPost("/records/{id:guid}/reject", (Guid id, RejectInput i, ReviewService svc, CancellationToken ct) => svc.Reject(id, i, ct));
        review.MapPost("/records/{id:guid}/overtime", (Guid id, OvertimeInput i, ReviewService svc, CancellationToken ct) =>
            svc.ApproveOvertime(id, i, ct));

        // The holiday list changes pay: the owner only.
        var holidays = app.MapGroup("/api/attendance/holidays").WithTags("Chấm công — ngày lễ").RequireAuthorization(Policies.Admin);
        holidays.MapPost("/", (HolidayInput i, HolidayService svc, CancellationToken ct) => svc.Save(null, i, ct));
        holidays.MapPut("/{id:guid}", (Guid id, HolidayInput i, HolidayService svc, CancellationToken ct) => svc.Save(id, i, ct));
        holidays.MapDelete("/{id:guid}", (Guid id, HolidayService svc, CancellationToken ct) => svc.Delete(id, ct));
    }
}
