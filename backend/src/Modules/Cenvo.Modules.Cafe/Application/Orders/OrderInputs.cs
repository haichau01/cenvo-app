namespace Cenvo.Modules.Cafe.Application.Orders;

public record CreateOrderInput(OrderType Type, Guid? TableId, string? CustomerName);
public record ToppingInput(Guid ProductId, decimal Quantity = 1);
public record AddLineInput(Guid ProductId, decimal Quantity, ToppingInput[]? Toppings, string? Note);
public record UpdateLineInput(decimal Quantity, string? Note);
/// <summary>A manager typing their own account on the cashier's device to approve a discount above the limit.</summary>
public record ManagerCredentials(string UserName, string Password);

public record DiscountInput(decimal? Amount, decimal? Percent, string? Reason, ManagerCredentials? Approval);
public record LineDiscountInput(decimal Amount, string? Reason, ManagerCredentials? Approval);
public record OrderInfoInput(string? CustomerName, string? Note);
public record PayInput(TenderedPayment[] Payments);
public record CancelInput(string Reason);
public record OrderHistoryFilter(DateOnly From, DateOnly To, OrderStatus? Status, string? Search);

/// <summary>Move / merge / split tables. Empty LineIds = the whole order.</summary>
public record MoveLinesInput(Guid[]? LineIds, Guid? ToTableId, Guid? ToOrderId);
