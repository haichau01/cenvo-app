namespace Cenvo.Modules.Cafe.Application.Setup;

public record AreaInput(string Name, int SortOrder);
public record TableInput(Guid AreaId, string Name, int Seats, int SortOrder);
public record MenuSettingsInput(bool IsTopping, bool ShowOnPos, int SortOrder);
public record RecipeInput(Guid IngredientProductId, Guid? UnitId, decimal Quantity);

public record TableDto(Guid Id, Guid AreaId, string Name, int Seats, int SortOrder, Guid? OpenOrderId,
    string? OpenOrderCode, decimal? OpenOrderTotal, DateTime? OpenedAt);
public record AreaDto(Guid Id, string Name, int SortOrder, List<TableDto> Tables);

public record MenuItemDto(Guid ProductId, string Code, string Name, Guid? CategoryId, string? CategoryName, int CategorySort,
    decimal Price, string? ImageUrl, bool IsTopping, bool ShowOnPos, int SortOrder, bool HasRecipe);

public record RecipeLineDto(Guid IngredientProductId, string IngredientCode, string IngredientName, Guid? UnitId,
    string UnitName, decimal Quantity);
