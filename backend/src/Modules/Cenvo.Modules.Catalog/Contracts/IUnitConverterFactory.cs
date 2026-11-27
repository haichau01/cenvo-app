namespace Cenvo.Modules.Catalog.Contracts;

/// <summary>Builds a unit converter for a set of products (one query).</summary>
public interface IUnitConverterFactory
{
    Task<UnitConverter> Create(IEnumerable<Guid> productIds, CancellationToken ct);
}
