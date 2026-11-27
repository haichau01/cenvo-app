namespace Cenvo.Modules.Catalog.Application.Products;

public record ProductUnitInput(Guid UnitId, decimal Factor);

public record ProductInput(string Code, string Name, ProductKind Kind, Guid? CategoryId, Guid BaseUnitId,
    decimal DefaultPrice, decimal MinStock, bool TrackStock, bool IsActive, string? ImageUrl, ProductUnitInput[]? Units);

public record ProductUnitDto(Guid UnitId, string UnitName, decimal Factor);

/// <remarks>Init properties (not a positional record): it is built inside EF projections, where C# forbids named arguments.</remarks>
public record ProductDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required ProductKind Kind { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public required Guid BaseUnitId { get; init; }
    public required string BaseUnitName { get; init; }
    public required decimal DefaultPrice { get; init; }
    /// <summary>Price at the current branch (branch override if any, otherwise the default price).</summary>
    public required decimal Price { get; init; }
    public required decimal MinStock { get; init; }
    public required bool TrackStock { get; init; }
    public required bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
    public required ProductUnitDto[] Units { get; init; }
}
