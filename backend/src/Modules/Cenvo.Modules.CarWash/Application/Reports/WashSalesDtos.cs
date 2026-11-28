namespace Cenvo.Modules.CarWash.Application.Reports;

/// <param name="Revenue">Money taken that day: wash tickets + packages sold.</param>
/// <param name="GrossProfit">Revenue − supplies issued − washers' commission.</param>
public record WashDailyRevenue(DateOnly Date, int Tickets, decimal WashRevenue, decimal MembershipRevenue, decimal Revenue,
    decimal SupplyCost, decimal Commission, decimal GrossProfit);

/// <remarks>Built in SQL → init properties.</remarks>
public record WashServiceSales
{
    public required Guid OfferingId { get; init; }
    public required string Name { get; init; }
    public required int Count { get; init; }
    /// <summary>How many of them a membership paid for.</summary>
    public required int MembershipUses { get; init; }
    /// <summary>Paid by the customer (membership-covered washes count 0).</summary>
    public required decimal Revenue { get; init; }
}

/// <remarks>Built in SQL → init properties.</remarks>
public record WashVehicleSales
{
    public required VehicleType VehicleType { get; init; }
    public required int Tickets { get; init; }
    public required decimal Revenue { get; init; }
}

/// <summary>
/// Car wash sales of the period (Vietnam days). Tickets count by payment time — like café orders — so the money matches
/// the shift reconciliation; supplies and commission are those of the same tickets.
/// </summary>
/// <param name="Revenue">Money taken: wash tickets + packages sold.</param>
/// <param name="MembershipCovered">List price of the washes paid with packages (sold earlier, not money of this period).</param>
/// <param name="GrossProfit">Revenue − supplies issued − washers' commission.</param>
public record WashSalesSummary(
    DateOnly From, DateOnly To,
    int Tickets, int Cancelled,
    decimal WashRevenue, int MembershipsSold, decimal MembershipRevenue, decimal Revenue,
    int MembershipUses, decimal MembershipCovered,
    decimal SupplyCost, decimal Commission, decimal GrossProfit, decimal AverageTicket,
    Dictionary<PaymentMethod, decimal> ByMethod,
    List<WashDailyRevenue> Daily,
    List<WashServiceSales> ByService,
    List<WashVehicleSales> ByVehicle);
