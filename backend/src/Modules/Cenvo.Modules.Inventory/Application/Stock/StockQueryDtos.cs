namespace Cenvo.Modules.Inventory.Application.Stock;

public record BalanceDto(Guid WarehouseId, string WarehouseName, Guid ProductId, string ProductCode, string ProductName,
    ProductKind Kind, string UnitName, decimal Quantity, decimal AverageCost, decimal Value, decimal MinStock, bool BelowMin);

public record StockCardRow(DateTime Date, string DocumentCode, StockDocumentType DocumentType, Guid DocumentId,
    bool IsReversal, decimal In, decimal Out, decimal UnitCost, decimal Balance);

public record StockCardDto(Guid ProductId, string ProductName, string UnitName, decimal Opening, decimal TotalIn,
    decimal TotalOut, decimal Closing, List<StockCardRow> Rows);

public record ConsumptionDto(Guid ProductId, string ProductCode, string ProductName, string UnitName, decimal Quantity, decimal Cost);
