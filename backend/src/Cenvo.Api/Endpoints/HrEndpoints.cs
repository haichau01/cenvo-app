using Cenvo.Modules.Hr.Application.Adjustments;
using Cenvo.Modules.Hr.Application.Employees;

namespace Cenvo.Api.Endpoints;

public static class HrEndpoints
{
    public static void MapHrEndpoints(this IEndpointRouteBuilder app)
    {
        // National ID, phone and pay: the owner (Admin) only.
        MapEmployees(app.MapGroup("/api/hr/employees").WithTags("Nhân sự").RequireAuthorization(Policies.Admin));
        MapAdjustments(app.MapGroup("/api/hr/adjustments").WithTags("Nhân sự — thưởng, phạt, tạm ứng").RequireAuthorization(Policies.Managers));
    }

    private static void MapEmployees(RouteGroupBuilder g)
    {
        g.MapGet("/", (EmployeeQueryService svc, CancellationToken ct) => svc.List(ct));
        g.MapGet("/{id:guid}", (Guid id, EmployeeQueryService svc, CancellationToken ct) => svc.Get(id, ct));
        g.MapPost("/", (EmployeeInput i, EmployeeService svc, CancellationToken ct) => svc.Save(null, i, ct));
        g.MapPut("/{id:guid}", (Guid id, EmployeeInput i, EmployeeService svc, CancellationToken ct) => svc.Save(id, i, ct));
        g.MapPost("/{id:guid}/leave", (Guid id, LeaveInput i, EmployeeService svc, CancellationToken ct) => svc.Leave(id, i, ct));
        g.MapPost("/{id:guid}/rehire", (Guid id, EmployeeService svc, CancellationToken ct) => svc.Rehire(id, ct));

        g.MapPost("/{id:guid}/contracts", (Guid id, ContractInput i, EmployeeService svc, CancellationToken ct) =>
            svc.AddContract(id, i, ct));
        g.MapPut("/{id:guid}/contracts/{contractId:guid}", (Guid id, Guid contractId, ContractInput i, EmployeeService svc,
            CancellationToken ct) => svc.UpdateContract(id, contractId, i, ct));
        g.MapPost("/{id:guid}/salary-rates", (Guid id, SalaryRateInput i, EmployeeService svc, CancellationToken ct) =>
            svc.AddSalaryRate(id, i, ct));
        g.MapDelete("/{id:guid}/salary-rates/{rateId:guid}", (Guid id, Guid rateId, EmployeeService svc, CancellationToken ct) =>
            svc.RemoveSalaryRate(id, rateId, ct));
    }

    private static void MapAdjustments(RouteGroupBuilder g)
    {
        g.MapGet("/", (DateOnly from, DateOnly to, AdjustmentQueryService svc, CancellationToken ct) => svc.List(from, to, ct));
        g.MapGet("/staff", (AdjustmentQueryService svc, CancellationToken ct) => svc.Staff(ct));
        g.MapPost("/", (AdjustmentInput i, AdjustmentService svc, CancellationToken ct) => svc.Record(i, ct));
        g.MapPost("/{id:guid}/cancel", (Guid id, CancelAdjustmentInput i, AdjustmentService svc, CancellationToken ct) =>
            svc.Cancel(id, i, ct));
    }
}
