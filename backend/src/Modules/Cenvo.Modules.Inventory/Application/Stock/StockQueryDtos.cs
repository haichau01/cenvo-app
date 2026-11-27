namespace Cenvo.Modules.Inventory.Application.Stock;

/// <remarks>Init properties (not a positional record): it is built inside EF projections, where C# forbids named arguments.</remarks>
public record BalanceDto
{
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductCode { get; init; }
    public required string ProductName { get; init; }
    public required ProductKind Kind { get; init; }
    public required string UnitName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal AverageCost { get; init; }
    public required decimal Value { get; init; }
    public required decimal MinStock { get; init; }
    public required bool BelowMin { get; init; }
}

public record StockCardRow(DateTime Date, string DocumentCode, StockDocumentType DocumentType, Guid DocumentId,
    bool IsReversal, decimal In, decimal Out, decimal UnitCost, decimal Balance);

public record StockCardDto(Guid ProductId, string ProductName, string UnitName, decimal Opening, decimal TotalIn,
    decimal TotalOut, decimal Closing, List<StockCardRow> Rows);

public record ConsumptionDto(Guid ProductId, string ProductCode, string ProductName, string UnitName, decimal Quantity, decimal Cost);
