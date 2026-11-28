namespace Cenvo.Modules.Cafe.Application.Orders;

public static class OrderMappings
{
    public static OrderDto ToDto(this CafeOrder o, string? tableName, string? shiftCode) => new(
        o.Id, o.Code, o.Type, o.Status, o.TableId, tableName, o.CustomerName, o.Note, o.Subtotal, o.DiscountAmount,
        o.Total, o.CashReceived, o.ChangeAmount, o.CreatedAt, o.PaidAt, o.CancelReason, shiftCode,
        o.Lines.OrderBy(l => l.SortOrder).Select(ToDto).ToList(),
        o.Payments.Select(p => new PaymentDto(p.Method, p.Amount, p.Reference)).ToList(),
        o.DiscountReason, o.DiscountApprovedByName, DiscountPolicy.CashierLimit(o.GrossAmount));

    private static LineDto ToDto(CafeOrderLine l) => new(
        l.Id, l.ProductId, l.Name, l.UnitPrice, l.Quantity, l.DiscountAmount, l.DiscountReason, l.Amount, l.Note, l.KitchenStatus, l.SentAt,
        l.Toppings.Select(t => new ToppingDto(t.ProductId, t.Name, t.UnitPrice, t.Quantity)).ToList());
}
