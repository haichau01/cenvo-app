using Cenvo.Modules.Catalog.Application.Categories;
using Cenvo.Modules.Catalog.Application.Products;
using Cenvo.Modules.Catalog.Application.Units;
using Cenvo.Modules.Catalog.Domain;

namespace Cenvo.Api.Endpoints;

public record NameInput(string Name);
public record PriceInput(decimal? Price);

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog").WithTags("Danh mục hàng hóa").RequireAuthorization();

        catalog.MapGet("/units", (UnitService svc, CancellationToken ct) => svc.List(ct));
        catalog.MapPost("/units", (NameInput i, UnitService svc, CancellationToken ct) => svc.Save(null, i.Name, ct)).RequireAuthorization(Policies.Storekeepers);
        catalog.MapPut("/units/{id:guid}", (Guid id, NameInput i, UnitService svc, CancellationToken ct) => svc.Save(id, i.Name, ct)).RequireAuthorization(Policies.Storekeepers);

        catalog.MapGet("/categories", (ProductKind? kind, CategoryService svc, CancellationToken ct) => svc.List(kind, ct));
        catalog.MapPost("/categories", (CategoryInput i, CategoryService svc, CancellationToken ct) => svc.Save(null, i, ct)).RequireAuthorization(Policies.Storekeepers);
        catalog.MapPut("/categories/{id:guid}", (Guid id, CategoryInput i, CategoryService svc, CancellationToken ct) => svc.Save(id, i, ct)).RequireAuthorization(Policies.Storekeepers);
        catalog.MapDelete("/categories/{id:guid}", (Guid id, CategoryService svc, CancellationToken ct) => svc.Delete(id, ct)).RequireAuthorization(Policies.Storekeepers);

        catalog.MapGet("/products", (ProductKind? kind, string? q, ProductQueryService svc, CancellationToken ct) => svc.List(kind, q, ct));
        catalog.MapPost("/products", (ProductInput i, ProductService svc, CancellationToken ct) => svc.Save(null, i, ct)).RequireAuthorization(Policies.Storekeepers);
        catalog.MapPut("/products/{id:guid}", (Guid id, ProductInput i, ProductService svc, CancellationToken ct) => svc.Save(id, i, ct)).RequireAuthorization(Policies.Storekeepers);
        catalog.MapDelete("/products/{id:guid}", (Guid id, ProductService svc, CancellationToken ct) => svc.Delete(id, ct)).RequireAuthorization(Policies.Storekeepers);
        catalog.MapPut("/products/{id:guid}/branch-price", (Guid id, PriceInput i, ProductService svc, CancellationToken ct) =>
            svc.SetBranchPrice(id, i.Price, ct)).RequireAuthorization(Policies.Managers);
    }
}
