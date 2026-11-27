using Cenvo.Modules.Organization.Application.Branches;
using Cenvo.Modules.Organization.Application.Users;

namespace Cenvo.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var branches = app.MapGroup("/api/branches").WithTags("Chi nhánh").RequireAuthorization();
        branches.MapGet("/", (BranchService svc, CancellationToken ct) => svc.List(ct));
        branches.MapPost("/", (BranchInput i, BranchService svc, CancellationToken ct) => svc.Save(null, i, ct)).RequireAuthorization(Policies.Admin);
        branches.MapPut("/{id:guid}", (Guid id, BranchInput i, BranchService svc, CancellationToken ct) => svc.Save(id, i, ct)).RequireAuthorization(Policies.Admin);

        var users = app.MapGroup("/api/users").WithTags("Người dùng").RequireAuthorization(Policies.Admin);
        users.MapGet("/", (UserService svc, CancellationToken ct) => svc.List(ct));
        users.MapPost("/", (UserInput i, UserService svc, CancellationToken ct) => svc.Save(null, i, ct));
        users.MapPut("/{id:guid}", (Guid id, UserInput i, UserService svc, CancellationToken ct) => svc.Save(id, i, ct));
    }
}
