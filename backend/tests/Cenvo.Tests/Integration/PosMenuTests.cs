namespace Cenvo.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PosMenuTests(ApiFixture api)
{
    [Fact]
    public async Task Cashier_gets_pos_menu_sorted_by_category()
    {
        var cashier = await api.LoginAs("thungan1");

        var menu = (await cashier.Get("/api/cafe/menu")).Arr();

        Assert.NotEmpty(menu);
        var sorts = menu.Select(m => m!["categorySort"]!.GetValue<int>()).ToList();
        Assert.Equal(sorts.Order(), sorts);
    }
}
