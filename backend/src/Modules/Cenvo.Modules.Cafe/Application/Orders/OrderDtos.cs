namespace Cenvo.Modules.Cafe.Application.Orders;

public record ToppingDto(Guid ProductId, string Name, decimal UnitPrice, decimal Quantity);

public record LineDto(Guid Id, Guid ProductId, string Name, decimal UnitPrice, decimal Quantity, decimal DiscountAmount,
    decimal Amount, string? Note, KitchenStatus KitchenStatus, DateTime? SentAt, List<ToppingDto> Toppings);

public record PaymentDto(PaymentMethod Method, decimal Amount, string? Reference);

public record OrderDto(Guid Id, string Code, OrderType Type, OrderStatus Status, Guid? TableId, string? TableName,
    string? CustomerName, string? Note, decimal Subtotal, decimal DiscountAmount, decimal Total, decimal CashReceived,
    decimal ChangeAmount, DateTime CreatedAt, DateTime? PaidAt, string? CancelReason, string? ShiftCode,
    List<LineDto> Lines, List<PaymentDto> Payments);
