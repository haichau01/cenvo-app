namespace Cenvo.Modules.Cafe.Domain;

/// <summary>Name + price effective at the branch at order time (captured onto the order line).</summary>
public record MenuPrice(Guid ProductId, string Name, decimal Price);
