namespace Cenvo.Modules.Inventory.Application.Documents;

public record StockDocumentLineInput(Guid ProductId, Guid? UnitId, decimal Quantity, decimal UnitPrice, string? Note);

public record StockDocumentInput(StockDocumentType Type, Guid WarehouseId, Guid? ToWarehouseId, Guid? SupplierId,
    DateTime? Date, string? Note, StockDocumentLineInput[] Lines);

public record StockDocumentFilter(StockDocumentType? Type, DocumentStatus? Status, DateOnly? From, DateOnly? To);
