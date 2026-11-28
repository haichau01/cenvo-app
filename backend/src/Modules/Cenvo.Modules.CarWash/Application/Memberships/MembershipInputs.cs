namespace Cenvo.Modules.CarWash.Application.Memberships;

public record SellMembershipInput(Guid PackageId, string CustomerPhone, string? CustomerName, TenderedPayment[] Payments);

public record CancelMembershipInput(string Reason);
