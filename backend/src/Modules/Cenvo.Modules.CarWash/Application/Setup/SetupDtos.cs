namespace Cenvo.Modules.CarWash.Application.Setup;

public record OfferingPriceInput(VehicleType VehicleType, decimal Price);
public record OfferingSupplyInput(Guid ProductId, Guid? UnitId, decimal Quantity);

public record OfferingInput(
    string Code, string Name, int DurationMinutes, CommissionType CommissionType, decimal CommissionValue,
    int SortOrder, bool IsActive, OfferingPriceInput[] Prices, OfferingSupplyInput[] Supplies);

public record OfferingPriceDto(VehicleType VehicleType, decimal Price);
public record OfferingSupplyDto(Guid ProductId, string ProductName, Guid? UnitId, string UnitName, decimal Quantity);

/// <remarks>Built in SQL → init properties (expression trees cannot use named arguments).</remarks>
public record OfferingDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int DurationMinutes { get; init; }
    public required CommissionType CommissionType { get; init; }
    public required decimal CommissionValue { get; init; }
    public required int SortOrder { get; init; }
    public required bool IsActive { get; init; }
    public required List<OfferingPriceDto> Prices { get; init; }
    public required List<OfferingSupplyDto> Supplies { get; init; }
}

public record PackageInput(
    string Code, string Name, Guid OfferingId, VehicleType? VehicleType, decimal Price, int Uses, int ValidDays, bool IsActive);

/// <remarks>Built in SQL → init properties.</remarks>
public record PackageDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required Guid OfferingId { get; init; }
    public required string OfferingName { get; init; }
    public required VehicleType? VehicleType { get; init; }
    public required decimal Price { get; init; }
    public required int Uses { get; init; }
    public required int ValidDays { get; init; }
    public required bool IsActive { get; init; }
}
