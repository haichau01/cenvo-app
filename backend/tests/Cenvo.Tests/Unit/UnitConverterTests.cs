using Cenvo.Modules.Catalog.Domain;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class UnitConverterTests
{
    private static readonly Guid Ml = Guid.NewGuid(), Liter = Guid.NewGuid(), Box = Guid.NewGuid(), Milk = Guid.NewGuid();
    private static readonly UnitConverter Conv = new(new Dictionary<Guid, UnitConverter.Info>
    {
        [Milk] = new("Sữa", Ml, true, new Dictionary<Guid, decimal> { [Liter] = 1000 }),
    });

    [Fact] public void Base_unit_or_null_keeps_quantity()
    {
        Assert.Equal(5m, Conv.ToBase(Milk, null, 5));
        Assert.Equal(5m, Conv.ToBase(Milk, Ml, 5));
    }

    [Fact] public void Converts_with_factor() => Assert.Equal(1500m, Conv.ToBase(Milk, Liter, 1.5m));

    [Fact] public void Unknown_unit_throws_business_error() =>
        Assert.Throws<BusinessException>(() => Conv.ToBase(Milk, Box, 1));
}
