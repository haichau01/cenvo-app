namespace Cenvo.Modules.Cafe.Application.Setup;

public record AreaInput(string Name, int SortOrder);
public record TableInput(Guid AreaId, string Name, int Seats, int SortOrder);
public record MenuSettingsInput(bool IsTopping, bool ShowOnPos, int SortOrder);
public record RecipeInput(Guid IngredientProductId, Guid? UnitId, decimal Quantity);

public record TableDto(Guid Id, Guid AreaId, string Name, int Seats, int SortOrder, Guid? OpenOrderId,
    string? OpenOrderCode, decimal? OpenOrderTotal, DateTime? OpenedAt);
public record AreaDto(Guid Id, string Name, int SortOrder, List<TableDto> Tables);

/// <remarks>Init properties (not a positional record): it is built inside EF projections, where C# forbids named arguments.</remarks>
public record MenuItemDto
{
    public required Guid ProductId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public required int CategorySort { get; init; }
    public required decimal Price { get; init; }
    public string? ImageUrl { get; init; }
    public required bool IsTopping { get; init; }
    public required bool ShowOnPos { get; init; }
    public required int SortOrder { get; init; }
    public required bool HasRecipe { get; init; }
}

public record RecipeLineDto(Guid IngredientProductId, string IngredientCode, string IngredientName, Guid? UnitId,
    string UnitName, decimal Quantity);
