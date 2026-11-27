namespace Cenvo.Modules.Catalog.Domain;

/// <summary>Converts quantities to the base unit. Pure (no database) for easy unit testing.</summary>
public class UnitConverter(IReadOnlyDictionary<Guid, UnitConverter.Info> products)
{
    public record Info(string Name, Guid BaseUnitId, bool TrackStock, IReadOnlyDictionary<Guid, decimal> Factors);

    public Info Product(Guid productId) =>
        products.TryGetValue(productId, out var p) ? p : throw new NotFoundException("sản phẩm", productId);

    public decimal ToBase(Guid productId, Guid? unitId, decimal quantity)
    {
        var product = Product(productId);
        if (unitId == null || unitId == product.BaseUnitId) return quantity;
        if (!product.Factors.TryGetValue(unitId.Value, out var factor))
            throw new BusinessException(CatalogErrors.MissingConversion(product.Name));
        return quantity * factor;
    }
}
