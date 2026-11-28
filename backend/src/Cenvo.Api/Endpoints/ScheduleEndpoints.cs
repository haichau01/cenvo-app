using Cenvo.Modules.Schedule.Application.Planning;
using Cenvo.Modules.Schedule.Application.Templates;

namespace Cenvo.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var plan = app.MapGroup("/api/schedule").WithTags("Lịch trực").RequireAuthorization(Policies.Managers);
        plan.MapGet("/templates", (TemplateService svc, CancellationToken ct) => svc.List(ct));
        plan.MapPost("/templates", (TemplateInput i, TemplateService svc, CancellationToken ct) => svc.Save(null, i, ct));
        plan.MapPut("/templates/{id:guid}", (Guid id, TemplateInput i, TemplateService svc, CancellationToken ct) => svc.Save(id, i, ct));
        plan.MapDelete("/templates/{id:guid}", (Guid id, TemplateService svc, CancellationToken ct) => svc.Delete(id, ct));
        plan.MapGet("/settings", (TemplateService svc, CancellationToken ct) => svc.Settings(ct));
        plan.MapPut("/settings", (ScheduleSettingsDto i, TemplateService svc, CancellationToken ct) => svc.SaveSettings(i, ct));

        plan.MapGet("/week", (DateOnly date, WeekQueryService svc, CancellationToken ct) => svc.Week(date, ct));
        plan.MapPost("/assignments", (AssignInput i, PlanningService svc, CancellationToken ct) => svc.Assign(i, ct));
        plan.MapDelete("/assignments/{id:guid}", (Guid id, PlanningService svc, CancellationToken ct) => svc.Remove(id, ct));

        // Everyone sees their own shifts (all branches); nobody else's.
        app.MapGet("/api/schedule/mine", (DateOnly from, DateOnly to, MyScheduleQueryService svc, CancellationToken ct) =>
            svc.Mine(from, to, ct)).WithTags("Lịch trực").RequireAuthorization();
    }
}
