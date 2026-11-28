using Cenvo.Modules.Cafe.Application.Kitchen;
using Cenvo.Modules.Cafe.Application.Orders;
using Cenvo.Modules.Cafe.Application.Reports;
using Cenvo.Modules.Cafe.Application.Setup;
using Cenvo.Modules.Cafe.Application.Shifts;
using Cenvo.Modules.Cafe.Domain;

namespace Cenvo.Api.Endpoints;

public static class CafeEndpoints
{
    public static void MapCafeEndpoints(this IEndpointRouteBuilder app)
    {
        MapSetup(app.MapGroup("/api/cafe").WithTags("Cà phê — thiết lập").RequireAuthorization());
        MapOrders(app.MapGroup("/api/cafe/orders").WithTags("Cà phê — hóa đơn").RequireAuthorization());
        MapKitchen(app.MapGroup("/api/cafe/kitchen").WithTags("Cà phê — pha chế").RequireAuthorization());
        MapShifts(app.MapGroup("/api/cafe/shifts").WithTags("Cà phê — ca").RequireAuthorization(Policies.Cashiers));

        app.MapGet("/api/reports/cafe-sales", (DateOnly from, DateOnly to, SalesReportService svc, CancellationToken ct) =>
            svc.Sales(from, to, ct)).WithTags("Báo cáo").RequireAuthorization(Policies.Managers);
    }

    private static void MapSetup(RouteGroupBuilder g)
    {
        g.MapGet("/areas", (AreaTableService svc, CancellationToken ct) => svc.FloorPlan(ct));
        g.MapPost("/areas", (AreaInput i, AreaTableService svc, CancellationToken ct) => svc.SaveArea(null, i, ct)).RequireAuthorization(Policies.Managers);
        g.MapPut("/areas/{id:guid}", (Guid id, AreaInput i, AreaTableService svc, CancellationToken ct) => svc.SaveArea(id, i, ct)).RequireAuthorization(Policies.Managers);
        g.MapDelete("/areas/{id:guid}", (Guid id, AreaTableService svc, CancellationToken ct) => svc.DeleteArea(id, ct)).RequireAuthorization(Policies.Managers);
        g.MapPost("/tables", (TableInput i, AreaTableService svc, CancellationToken ct) => svc.SaveTable(null, i, ct)).RequireAuthorization(Policies.Managers);
        g.MapPut("/tables/{id:guid}", (Guid id, TableInput i, AreaTableService svc, CancellationToken ct) => svc.SaveTable(id, i, ct)).RequireAuthorization(Policies.Managers);
        g.MapDelete("/tables/{id:guid}", (Guid id, AreaTableService svc, CancellationToken ct) => svc.DeleteTable(id, ct)).RequireAuthorization(Policies.Managers);

        g.MapGet("/menu", (bool? all, MenuQueryService svc, CancellationToken ct) => all == true ? svc.FullMenu(ct) : svc.PosMenu(ct));
        g.MapPut("/menu/{productId:guid}", (Guid productId, MenuSettingsInput i, MenuService svc, CancellationToken ct) =>
            svc.SaveSettings(productId, i, ct)).RequireAuthorization(Policies.Managers);
        g.MapGet("/menu/{productId:guid}/recipe", (Guid productId, RecipeService svc, CancellationToken ct) => svc.Get(productId, ct));
        g.MapPut("/menu/{productId:guid}/recipe", (Guid productId, RecipeInput[] lines, RecipeService svc, CancellationToken ct) =>
            svc.Save(productId, lines, ct)).RequireAuthorization(Policies.Managers);
    }

    private static void MapOrders(RouteGroupBuilder g)
    {
        g.MapGet("/open", (OrderQueryService svc, CancellationToken ct) => svc.OpenOrders(ct));
        g.MapGet("/", ([AsParameters] OrderHistoryFilter f, OrderQueryService svc, CancellationToken ct) => svc.History(f, ct));
        g.MapGet("/{id:guid}", (Guid id, OrderQueryService svc, CancellationToken ct) => svc.Get(id, ct));
        g.MapGet("/{id:guid}/receipt", (Guid id, ReceiptQueryService svc, CancellationToken ct) => svc.Get(id, ct));

        g.MapPost("/", (CreateOrderInput i, OrderEditingService svc, CancellationToken ct) => svc.Create(i, ct));
        g.MapPut("/{id:guid}", (Guid id, OrderInfoInput i, OrderEditingService svc, CancellationToken ct) => svc.UpdateInfo(id, i, ct));
        g.MapPost("/{id:guid}/lines", (Guid id, AddLineInput i, OrderEditingService svc, CancellationToken ct) => svc.AddLine(id, i, ct));
        g.MapPut("/{id:guid}/lines/{lineId:guid}", (Guid id, Guid lineId, UpdateLineInput i, OrderEditingService svc, CancellationToken ct) =>
            svc.UpdateLine(id, lineId, i, ct));
        g.MapDelete("/{id:guid}/lines/{lineId:guid}", (Guid id, Guid lineId, OrderEditingService svc, CancellationToken ct) =>
            svc.RemoveLine(id, lineId, ct));
        // Carries manager credentials when approving → limited like the login endpoint.
        g.MapPut("/{id:guid}/discount", (Guid id, DiscountInput i, DiscountService svc, CancellationToken ct) => svc.DiscountOrder(id, i, ct))
            .RequireRateLimiting(RateLimits.Credentials);
        g.MapPut("/{id:guid}/lines/{lineId:guid}/discount", (Guid id, Guid lineId, LineDiscountInput i, DiscountService svc, CancellationToken ct) =>
            svc.DiscountLine(id, lineId, i, ct)).RequireRateLimiting(RateLimits.Credentials);

        g.MapPost("/{id:guid}/move", (Guid id, MoveLinesInput i, TableTransferService svc, CancellationToken ct) => svc.Move(id, i, ct));
        g.MapPost("/{id:guid}/send", (Guid id, KitchenService svc, CancellationToken ct) => svc.Send(id, ct));
        g.MapPost("/{id:guid}/pay", (Guid id, PayInput i, CheckoutService svc, CancellationToken ct) => svc.Pay(id, i, ct))
            .RequireAuthorization(Policies.Cashiers);
        g.MapPost("/{id:guid}/cancel", (Guid id, CancelInput i, CheckoutService svc, CancellationToken ct) => svc.Cancel(id, i, ct));
    }

    private static void MapKitchen(RouteGroupBuilder g)
    {
        g.MapGet("/", (KitchenService svc, CancellationToken ct) => svc.Queue(ct));
        g.MapPut("/{lineId:guid}", (Guid lineId, KitchenStatusInput i, KitchenService svc, CancellationToken ct) =>
            svc.SetStatus(lineId, i.Status, ct));
    }

    private static void MapShifts(RouteGroupBuilder g)
    {
        g.MapGet("/current", async (ShiftService svc, CancellationToken ct) =>
            await svc.CurrentShift(ct) is { } shift ? await svc.Summary(shift.Id, ct) : null);
        g.MapPost("/open", (OpenShiftInput i, ShiftService svc, CurrentUser me, CancellationToken ct) => svc.Open(i.OpeningCash, me.FullName, ct));
        g.MapPost("/close", (CloseShiftInput i, ShiftService svc, CancellationToken ct) => svc.Close(i, ct));
        g.MapGet("/", (DateOnly from, DateOnly to, ShiftService svc, CancellationToken ct) => svc.List(from, to, ct));
        g.MapGet("/{id:guid}", (Guid id, ShiftService svc, CancellationToken ct) => svc.Summary(id, ct));
    }
}
