using Cenvo.Modules.Inventory.Application.Documents;
using Cenvo.Modules.Inventory.Application.Stock;
using Cenvo.Modules.Inventory.Application.Suppliers;
using Cenvo.Modules.Inventory.Application.Warehouses;

namespace Cenvo.Api.Endpoints;

public record ReasonInput(string Reason);

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var inventory = app.MapGroup("/api/inventory").WithTags("Kho");

        // Every signed-in user needs to read inventory (warehouse picker, low-stock alerts on the dashboard).
        inventory.MapGet("/warehouses", (bool? all, WarehouseService svc, CancellationToken ct) => svc.List(all == true, ct)).RequireAuthorization();
        inventory.MapGet("/low-stock", (StockQueryService svc, CancellationToken ct) => svc.LowStock(ct)).RequireAuthorization();

        var keepers = inventory.MapGroup("/").RequireAuthorization(Policies.Storekeepers);
        keepers.MapPost("/warehouses", (WarehouseInput i, WarehouseService svc, CancellationToken ct) => svc.Save(null, i, ct));
        keepers.MapPut("/warehouses/{id:guid}", (Guid id, WarehouseInput i, WarehouseService svc, CancellationToken ct) => svc.Save(id, i, ct));

        keepers.MapGet("/suppliers", (SupplierService svc, CancellationToken ct) => svc.List(ct));
        keepers.MapPost("/suppliers", (SupplierInput i, SupplierService svc, CancellationToken ct) => svc.Save(null, i, ct));
        keepers.MapPut("/suppliers/{id:guid}", (Guid id, SupplierInput i, SupplierService svc, CancellationToken ct) => svc.Save(id, i, ct));

        keepers.MapGet("/documents", ([AsParameters] StockDocumentFilter f, StockDocumentQueryService svc, CancellationToken ct) => svc.List(f, ct));
        keepers.MapGet("/documents/incoming", (StockDocumentQueryService svc, CancellationToken ct) => svc.IncomingTransfers(ct));
        keepers.MapGet("/documents/{id:guid}", (Guid id, StockDocumentQueryService svc, CancellationToken ct) => svc.Get(id, ct));
        keepers.MapPost("/documents", (StockDocumentInput i, StockDocumentService svc, CancellationToken ct) => svc.CreateDraft(i, ct));
        keepers.MapPut("/documents/{id:guid}", (Guid id, StockDocumentInput i, StockDocumentService svc, CancellationToken ct) => svc.UpdateDraft(id, i, ct));
        keepers.MapDelete("/documents/{id:guid}", (Guid id, StockDocumentService svc, CancellationToken ct) => svc.DeleteDraft(id, ct));
        keepers.MapPost("/documents/{id:guid}/post", (Guid id, StockDocumentService svc, CancellationToken ct) => svc.Post(id, ct));
        keepers.MapPost("/documents/{id:guid}/receive", (Guid id, StockDocumentService svc, CancellationToken ct) => svc.Receive(id, ct));
        keepers.MapPost("/documents/{id:guid}/cancel", (Guid id, ReasonInput i, StockDocumentService svc, CancellationToken ct) =>
            svc.Cancel(id, i.Reason, ct)).RequireAuthorization(Policies.Managers);

        keepers.MapGet("/balances", (Guid? warehouseId, string? q, bool? belowMin, StockQueryService svc, CancellationToken ct) =>
            svc.Balances(warehouseId, q, belowMin == true, ct));
        keepers.MapGet("/stock-card", (Guid productId, Guid warehouseId, DateOnly from, DateOnly to, StockQueryService svc, CancellationToken ct) =>
            svc.StockCard(productId, warehouseId, from, to, ct));
        keepers.MapGet("/consumption", (DateOnly from, DateOnly to, StockQueryService svc, CancellationToken ct) => svc.Consumption(from, to, ct));
    }
}
