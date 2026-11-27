namespace Cenvo.Modules.Catalog.Application.Products;

public record ProductUnitInput(Guid UnitId, decimal Factor);

public record ProductInput(string Code, string Name, ProductKind Kind, Guid? CategoryId, Guid BaseUnitId,
    decimal DefaultPrice, decimal MinStock, bool TrackStock, bool IsActive, string? ImageUrl, ProductUnitInput[]? Units);

public record ProductUnitDto(Guid UnitId, string UnitName, decimal Factor);

/// <param name="Price">Price at the current branch (branch override if any, otherwise the default price).</param>
public record ProductDto(Guid Id, string Code, string Name, ProductKind Kind, Guid? CategoryId, string? CategoryName,
    Guid BaseUnitId, string BaseUnitName, decimal DefaultPrice, decimal Price, decimal MinStock, bool TrackStock,
    bool IsActive, string? ImageUrl, ProductUnitDto[] Units);
