namespace Cenvo.Modules.Cafe.Application.Orders;

public record CreateOrderInput(OrderType Type, Guid? TableId, string? CustomerName);
public record ToppingInput(Guid ProductId, decimal Quantity = 1);
public record AddLineInput(Guid ProductId, decimal Quantity, ToppingInput[]? Toppings, string? Note);
public record UpdateLineInput(decimal Quantity, string? Note, decimal DiscountAmount);
public record DiscountInput(decimal? Amount, decimal? Percent);
public record OrderInfoInput(string? CustomerName, string? Note);
public record PayInput(TenderedPayment[] Payments);
public record CancelInput(string Reason);
public record OrderHistoryFilter(DateOnly From, DateOnly To, OrderStatus? Status, string? Search);

/// <summary>Move / merge / split tables. Empty LineIds = the whole order.</summary>
public record MoveLinesInput(Guid[]? LineIds, Guid? ToTableId, Guid? ToOrderId);
