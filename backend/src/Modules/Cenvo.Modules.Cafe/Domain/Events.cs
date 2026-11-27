namespace Cenvo.Modules.Cafe.Domain;

/// <summary>An item/topping sold (quantity in the selling unit, recipe not yet expanded).</summary>
public record SoldItem(Guid ProductId, decimal Quantity);

public record CafeOrderPaid(Guid OrderId, Guid BranchId, string Code, IReadOnlyList<SoldItem> Items) : IDomainEvent;

public record CafeOrderCancelled(Guid OrderId, string Code, string Reason) : IDomainEvent;
