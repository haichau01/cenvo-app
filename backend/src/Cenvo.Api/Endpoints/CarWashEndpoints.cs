using Cenvo.Modules.CarWash.Application.Customers;
using Cenvo.Modules.CarWash.Application.Memberships;
using Cenvo.Modules.CarWash.Application.Printing;
using Cenvo.Modules.CarWash.Application.Reminders;
using Cenvo.Modules.CarWash.Application.Reports;
using Cenvo.Modules.CarWash.Application.Setup;
using Cenvo.Modules.CarWash.Application.Tickets;
using Microsoft.AspNetCore.Mvc;

namespace Cenvo.Api.Endpoints;

public static class CarWashEndpoints
{
    /// <summary>Upload limit for one photo request (the photo itself is capped at 5 MB, plus multipart overhead).</summary>
    private const long PhotoRequestLimit = 6 * 1024 * 1024;

    public static void MapCarWashEndpoints(this IEndpointRouteBuilder app)
    {
        MapSetup(app.MapGroup("/api/carwash").WithTags("Rửa xe — thiết lập").RequireAuthorization(Policies.WashStaff));
        MapCustomers(app.MapGroup("/api/carwash").WithTags("Rửa xe — khách hàng").RequireAuthorization(Policies.WashStaff));
        MapTickets(app.MapGroup("/api/carwash/tickets").WithTags("Rửa xe — phiếu").RequireAuthorization(Policies.WashStaff));
        MapReminders(app.MapGroup("/api/carwash/reminders").WithTags("Rửa xe — nhắc khách").RequireAuthorization(Policies.Managers));

        app.MapGet("/api/reports/carwash-sales", (DateOnly from, DateOnly to, WashSalesReportService svc, CancellationToken ct) =>
            svc.Sales(from, to, ct)).WithTags("Báo cáo").RequireAuthorization(Policies.Managers);
        app.MapGet("/api/reports/carwash-commissions", (DateOnly from, DateOnly to, CommissionQueryService svc, CancellationToken ct) =>
            svc.Summary(from, to, ct)).WithTags("Báo cáo").RequireAuthorization(Policies.Managers);
    }

    private static void MapSetup(RouteGroupBuilder g)
    {
        g.MapGet("/services", (bool? all, OfferingQueryService svc, CancellationToken ct) => svc.List(all == true, ct));
        g.MapPost("/services", (OfferingInput i, OfferingService svc, CancellationToken ct) => svc.Save(null, i, ct))
            .RequireAuthorization(Policies.Managers);
        g.MapPut("/services/{id:guid}", (Guid id, OfferingInput i, OfferingService svc, CancellationToken ct) => svc.Save(id, i, ct))
            .RequireAuthorization(Policies.Managers);
        g.MapDelete("/services/{id:guid}", (Guid id, OfferingService svc, CancellationToken ct) => svc.Delete(id, ct))
            .RequireAuthorization(Policies.Managers);

        g.MapGet("/packages", (bool? all, PackageService svc, CancellationToken ct) => svc.List(all == true, ct));
        g.MapPost("/packages", (PackageInput i, PackageService svc, CancellationToken ct) => svc.Save(null, i, ct))
            .RequireAuthorization(Policies.Managers);
        g.MapPut("/packages/{id:guid}", (Guid id, PackageInput i, PackageService svc, CancellationToken ct) => svc.Save(id, i, ct))
            .RequireAuthorization(Policies.Managers);

        g.MapGet("/staff", (TicketQueryService svc, CancellationToken ct) => svc.Washers(ct));
    }

    private static void MapCustomers(RouteGroupBuilder g)
    {
        g.MapGet("/customers", (string? q, CustomerQueryService svc, CancellationToken ct) => svc.Search(q, ct));
        g.MapGet("/customers/{id:guid}", (Guid id, CustomerQueryService svc, CancellationToken ct) => svc.Get(id, ct));
        // Phone number = identity for memberships: only the counter (cashier/manager) may change it, never the bay staff.
        g.MapPut("/customers/{id:guid}", (Guid id, CustomerInput i, CustomerService svc, CancellationToken ct) => svc.Update(id, i, ct))
            .RequireAuthorization(Policies.Cashiers);
        // The customer asks at the counter to stop (or restart) reminder messages.
        g.MapPut("/customers/{id:guid}/reminders", (Guid id, SetAcceptsRemindersInput i, ReminderService svc, CancellationToken ct) =>
            svc.SetAcceptsReminders(id, i, ct)).RequireAuthorization(Policies.Cashiers);
        g.MapGet("/vehicles/lookup", (string plate, CustomerQueryService svc, CancellationToken ct) => svc.LookupPlate(plate, ct));

        g.MapPost("/memberships", (SellMembershipInput i, MembershipService svc, CancellationToken ct) => svc.Sell(i, ct))
            .RequireAuthorization(Policies.Cashiers);
        g.MapGet("/memberships/{id:guid}/receipt", (Guid id, WashPrintQueryService svc, CancellationToken ct) =>
            svc.Membership(id, ct)).RequireAuthorization(Policies.Cashiers);
        g.MapPost("/memberships/{id:guid}/cancel", (Guid id, CancelMembershipInput i, MembershipService svc, CancellationToken ct) =>
            svc.Cancel(id, i, ct)).RequireAuthorization(Policies.Managers);
    }

    private static void MapTickets(RouteGroupBuilder g)
    {
        g.MapGet("/queue", (TicketQueryService svc, CancellationToken ct) => svc.Queue(ct));
        g.MapGet("/", ([AsParameters] TicketHistoryFilter f, TicketQueryService svc, CancellationToken ct) => svc.History(f, ct));
        g.MapGet("/{id:guid}", (Guid id, TicketQueryService svc, CancellationToken ct) => svc.Get(id, ct));
        g.MapGet("/{id:guid}/receipt", (Guid id, WashPrintQueryService svc, CancellationToken ct) => svc.Ticket(id, ct));

        g.MapPost("/", (CreateTicketInput i, TicketIntakeService svc, CancellationToken ct) => svc.Create(i, ct));
        g.MapPost("/{id:guid}/services", (Guid id, TicketServiceInput i, TicketIntakeService svc, CancellationToken ct) =>
            svc.AddService(id, i, ct));
        g.MapDelete("/{id:guid}/services/{lineId:guid}", (Guid id, Guid lineId, TicketIntakeService svc, CancellationToken ct) =>
            svc.RemoveService(id, lineId, ct));
        g.MapPut("/{id:guid}/staff", (Guid id, AssignStaffInput i, TicketIntakeService svc, CancellationToken ct) =>
            svc.AssignStaff(id, i, ct));

        g.MapPost("/{id:guid}/start", (Guid id, TicketFlowService svc, CancellationToken ct) => svc.Start(id, ct));
        g.MapPost("/{id:guid}/return", (Guid id, TicketFlowService svc, CancellationToken ct) => svc.ReturnToWaiting(id, ct));
        g.MapPost("/{id:guid}/complete", (Guid id, TicketFlowService svc, CancellationToken ct) => svc.Complete(id, ct));
        g.MapPost("/{id:guid}/deliver", (Guid id, TicketFlowService svc, CancellationToken ct) => svc.Deliver(id, ct));

        g.MapPost("/{id:guid}/pay", (Guid id, PayTicketInput i, TicketCheckoutService svc, CancellationToken ct) => svc.Pay(id, i, ct))
            .RequireAuthorization(Policies.Cashiers);
        g.MapPost("/{id:guid}/cancel", (Guid id, CancelTicketInput i, TicketCheckoutService svc, CancellationToken ct) =>
            svc.Cancel(id, i, ct));

        MapPhotos(g);
    }

    /// <summary>Managers only: messages cost money and use customers' phone numbers.</summary>
    private static void MapReminders(RouteGroupBuilder g)
    {
        g.MapGet("/settings", (ReminderQueryService svc, CancellationToken ct) => svc.Settings(ct));
        g.MapPut("/settings", (ReminderSettingsInput i, ReminderService svc, CancellationToken ct) => svc.SaveSettings(i, ct));
        g.MapGet("/due", (ReminderQueryService svc, CancellationToken ct) => svc.Due(ct));
        g.MapGet("/history", (DateOnly from, DateOnly to, ReminderQueryService svc, CancellationToken ct) => svc.History(from, to, ct));
        g.MapPost("/send", (SendRemindersInput i, ReminderService svc, CancellationToken ct) => svc.Send(i, ct));
    }

    private static void MapPhotos(RouteGroupBuilder g)
    {
        // JWT in the Authorization header (no cookies) → no CSRF risk, so the form needs no antiforgery token.
        g.MapPost("/{id:guid}/photos", (Guid id, IFormFile file, TicketPhotoService svc, CancellationToken ct) =>
                svc.Upload(id, file.OpenReadStream(), file.Length, ct))
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(PhotoRequestLimit));
        g.MapGet("/{id:guid}/photos/{photoId:guid}", async (Guid id, Guid photoId, TicketPhotoService svc, CancellationToken ct) =>
        {
            var photo = await svc.Open(id, photoId, ct);
            return Results.File(photo.Content, photo.ContentType);
        });
        g.MapDelete("/{id:guid}/photos/{photoId:guid}", (Guid id, Guid photoId, TicketPhotoService svc, CancellationToken ct) =>
            svc.Delete(id, photoId, ct));
    }
}
