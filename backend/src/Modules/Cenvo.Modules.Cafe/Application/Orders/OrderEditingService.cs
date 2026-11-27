namespace Cenvo.Modules.Cafe.Application.Orders;

/// <summary>Opens orders and edits items on an open order.</summary>
public class OrderEditingService(
    AppDbContext db,
    OrderQueryService queries,
    IDocumentNumberGenerator numbers,
    IKitchenNotifier notifier,
    ICurrentUser user)
{
    public async Task<OrderDto> Create(CreateOrderInput input, CancellationToken ct)
    {
        var branchId = user.BranchId ?? throw new BusinessException(CafeErrors.NoBranch);
        if (input.Type == OrderType.DineIn && input.TableId != null)
        {
            await db.EnsureTableExists(input.TableId.Value, ct);
            // Table already has guests → reopen the existing order instead of creating a second one.
            var existing = await db.FindOpenOrderAtTable(input.TableId.Value, ct);
            if (existing != null) return await queries.Get(existing.Id, ct);
        }

        var code = await numbers.Next(branchId, CafeOrder.CodePrefix, DateTime.UtcNow, ct);
        var order = CafeOrder.Open(branchId, code, input.Type, input.TableId, input.CustomerName);
        db.Add(order);
        return await Save(order, ct);
    }

    public async Task<OrderDto> AddLine(Guid orderId, AddLineInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        var toppingInputs = input.Toppings?.Where(t => t.Quantity > 0).ToList() ?? [];
        var prices = await MenuPrices(toppingInputs.Select(t => t.ProductId).Append(input.ProductId), ct);

        var item = prices.GetValueOrDefault(input.ProductId) ?? throw new BusinessException(CafeErrors.ItemNotOnMenu);
        var toppings = toppingInputs.Select(t => CafeOrderLineTopping.From(
            prices.GetValueOrDefault(t.ProductId) ?? throw new BusinessException(CafeErrors.InvalidTopping), t.Quantity)).ToList();

        order.AddLine(item, input.Quantity, toppings, input.Note);
        return await Save(order, ct);
    }

    public async Task<OrderDto> UpdateLine(Guid orderId, Guid lineId, UpdateLineInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        order.UpdateLine(lineId, input.Quantity, input.Note, input.DiscountAmount);
        return await Save(order, ct);
    }

    public async Task<OrderDto> RemoveLine(Guid orderId, Guid lineId, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        order.RemoveLine(lineId, byManager: user.IsManager());
        return await Save(order, ct);
    }

    public async Task<OrderDto> SetDiscount(Guid orderId, DiscountInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        order.ApplyDiscount(input.Amount, input.Percent);
        return await Save(order, ct);
    }

    public async Task<OrderDto> UpdateInfo(Guid orderId, OrderInfoInput input, CancellationToken ct)
    {
        var order = await db.LoadOrder(orderId, ct);
        order.UpdateInfo(input.CustomerName, input.Note);
        return await Save(order, ct);
    }

    private async Task<OrderDto> Save(CafeOrder order, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await notifier.OrdersChanged(order.BranchId, ct);
        return await queries.Get(order.Id, ct);
    }

    /// <summary>Price at the current branch (ProductPrice is already branch-filtered), active items only.</summary>
    private async Task<Dictionary<Guid, MenuPrice?>> MenuPrices(IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToList();
        return await db.Set<Product>().AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.IsActive && p.Kind == ProductKind.FinishedGood)
            .Select(p => new MenuPrice(p.Id, p.Name,
                db.Set<ProductPrice>().Where(x => x.ProductId == p.Id).Select(x => (decimal?)x.Price).FirstOrDefault() ?? p.DefaultPrice))
            .ToDictionaryAsync(p => p.ProductId, p => (MenuPrice?)p, ct);
    }
}
