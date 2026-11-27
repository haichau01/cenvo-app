namespace Cenvo.Modules.Inventory.Application.Documents;

public record StockDocumentLineDto(Guid Id, Guid ProductId, string ProductCode, string ProductName, Guid? UnitId,
    string UnitName, decimal Quantity, decimal BaseQuantity, decimal? SystemQuantity, decimal UnitPrice, decimal Amount,
    string? Note);

public record StockDocumentDto(Guid Id, string Code, StockDocumentType Type, DocumentStatus Status, DateTime Date,
    Guid WarehouseId, string WarehouseName, Guid? ToWarehouseId, string? ToWarehouseName, TransferState TransferState,
    Guid? SupplierId, string? SupplierName, string? Note, string? RefType, string? RefCode, string? CancelReason,
    decimal TotalAmount, DateTime CreatedAt, List<StockDocumentLineDto>? Lines);
