using Cenvo.Infrastructure.Persistence;
using Cenvo.Modules.Cafe.Domain;
using Cenvo.Modules.Catalog.Domain;
using Cenvo.Modules.Inventory.Application.Documents;
using Cenvo.Modules.Inventory.Domain;
using Cenvo.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Api.Seed;

/// <summary>Sample data: 2 branches, users, ingredients, menu + recipes, tables, opening stock. Runs only on an empty database.</summary>
public static class DataSeeder
{
    public const string DefaultPassword = "123456";
    public const string AdminPassword = "Admin@123";

    public static async Task Run(IServiceProvider root, CancellationToken ct = default)
    {
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;
        var currentUser = sp.GetRequiredService<CurrentUser>();
        currentUser.RunAs(SystemUser.AllBranches);
        var db = sp.GetRequiredService<AppDbContext>();
        if (await db.Set<Branch>().AnyAsync(ct)) return;

        var branches = SeedBranchesAndUsers(db);
        var catalog = SeedCatalog(db);
        SeedMenu(db, catalog);
        var warehouses = SeedFloorPlansAndWarehouses(db, branches);
        db.Add(new Supplier { Code = "NCC-001", Name = "Công ty TNHH Nguyên liệu Pha chế Sài Gòn", Phone = "0909000111" });
        await db.SaveChangesAsync(ct);

        var documents = sp.GetRequiredService<StockDocumentService>();
        foreach (var warehouse in warehouses)
        {
            currentUser.RunAs(SystemUser.ForBranch(warehouse.BranchId));
            await SeedOpeningStock(documents, warehouse.Id, catalog, ct);
        }
    }

    /// <summary>Seeded catalog, looked up by unit name / product code.</summary>
    private sealed class SeededCatalog
    {
        public Dictionary<string, Unit> Units { get; } = [];
        public Dictionary<string, Product> Products { get; } = [];
        public Guid Unit(string name) => Units[name].Id;
        public Product this[string code] => Products[code];
    }

    private static Branch[] SeedBranchesAndUsers(AppDbContext db)
    {
        var q1 = new Branch { Code = "CN01", Name = "Cenvo Quận 1", Address = "12 Lê Lợi, Q.1, TP.HCM", Phone = "0281234567" };
        var thuDuc = new Branch { Code = "CN02", Name = "Cenvo Thủ Đức", Address = "88 Võ Văn Ngân, TP. Thủ Đức", Phone = "0287654321" };
        db.AddRange(q1, thuDuc);

        AppUser User(string userName, string fullName, string role, string password, params Branch[] branches)
        {
            var user = new AppUser { UserName = userName, FullName = fullName };
            user.SetPassword(password);
            user.SetRoles([role]);
            user.SetBranches(branches.Select(b => b.Id));
            return user;
        }
        db.AddRange(
            User("admin", "Quản trị hệ thống", Roles.Admin, AdminPassword),
            User("quanly1", "Nguyễn Văn Quản", Roles.BranchManager, DefaultPassword, q1),
            User("thungan1", "Trần Thị Thu", Roles.Cashier, DefaultPassword, q1),
            User("phache1", "Lê Văn Pha", Roles.Barista, DefaultPassword, q1),
            User("thukho1", "Phạm Thị Kho", Roles.Storekeeper, DefaultPassword, q1),
            User("quanly2", "Võ Minh Quân", Roles.BranchManager, DefaultPassword, thuDuc),
            User("thungan2", "Đặng Thu Ngân", Roles.Cashier, DefaultPassword, thuDuc));
        return [q1, thuDuc];
    }

    private static SeededCatalog SeedCatalog(AppDbContext db)
    {
        var catalog = new SeededCatalog();
        foreach (var name in new[] { "g", "kg", "ml", "lít", "ly", "cái", "hộp", "thùng", "chai", "gói" })
            catalog.Units[name] = new Unit { Name = name };
        db.AddRange(catalog.Units.Values);

        Category Category(string name, ProductKind kind, int sortOrder)
        {
            var category = new Category { Name = name, Kind = kind, SortOrder = sortOrder };
            db.Add(category);
            return category;
        }
        var coffeeTea = Category("Cà phê & trà", ProductKind.Ingredient, 1);
        var dairy = Category("Sữa & kem", ProductKind.Ingredient, 2);
        var sweeteners = Category("Đường & syrup", ProductKind.Ingredient, 3);
        var packaging = Category("Bao bì", ProductKind.Ingredient, 4);
        var menuCoffee = Category("Cà phê", ProductKind.FinishedGood, 1);
        var menuTea = Category("Trà", ProductKind.FinishedGood, 2);
        var menuOther = Category("Đồ uống khác", ProductKind.FinishedGood, 3);
        var menuTopping = Category("Topping", ProductKind.FinishedGood, 9);

        void Add(string code, string name, ProductKind kind, Category category, string baseUnit,
            decimal price, decimal minStock, bool trackStock, params (string Unit, decimal Factor)[] conversions)
        {
            var product = new Product
            {
                Code = code, Name = name, Kind = kind, CategoryId = category.Id, BaseUnitId = catalog.Unit(baseUnit),
                DefaultPrice = price, MinStock = minStock, TrackStock = trackStock,
            };
            product.SetUnits(conversions.Select(c => (catalog.Unit(c.Unit), c.Factor)));
            db.Add(product);
            catalog.Products[code] = product;
        }
        void Ingredient(string code, string name, Category category, string baseUnit, decimal minStock,
            params (string, decimal)[] conversions) =>
            Add(code, name, ProductKind.Ingredient, category, baseUnit, 0, minStock, true, conversions);
        void MenuItem(string code, string name, Category category, decimal price) =>
            Add(code, name, ProductKind.FinishedGood, category, "ly", price, 0, false);

        Ingredient("NL-CF-ROB", "Cà phê Robusta rang xay", coffeeTea, "g", 2000, ("kg", 1000));
        Ingredient("NL-CF-ARA", "Cà phê Arabica", coffeeTea, "g", 1000, ("kg", 1000));
        Ingredient("NL-TRA-DEN", "Trà đen", coffeeTea, "g", 500, ("kg", 1000), ("gói", 500));
        Ingredient("NL-SUA-DAC", "Sữa đặc Ông Thọ", dairy, "g", 3800, ("hộp", 380), ("thùng", 380 * 48));
        Ingredient("NL-SUA-TUOI", "Sữa tươi không đường", dairy, "ml", 3000, ("lít", 1000), ("hộp", 1000));
        Ingredient("NL-KEM-BEO", "Kem béo pha chế", dairy, "ml", 1000, ("lít", 1000));
        Ingredient("NL-DUONG", "Đường cát", sweeteners, "g", 2000, ("kg", 1000));
        Ingredient("NL-SYRUP-DAO", "Syrup đào", sweeteners, "ml", 750, ("chai", 750));
        Ingredient("NL-DAO-NGAM", "Đào ngâm", sweeteners, "g", 820, ("hộp", 820));
        Ingredient("NL-TRAN-CHAU", "Trân châu đen", sweeteners, "g", 1000, ("kg", 1000), ("gói", 1000));
        Ingredient("BB-LY-500", "Ly nhựa 500ml + nắp", packaging, "cái", 200, ("thùng", 1000));

        MenuItem("CF-DEN", "Cà phê đen đá", menuCoffee, 25000);
        MenuItem("CF-SUA", "Cà phê sữa đá", menuCoffee, 29000);
        MenuItem("CF-BACXIU", "Bạc xỉu", menuCoffee, 32000);
        MenuItem("CF-LATTE", "Latte nóng", menuCoffee, 45000);
        MenuItem("TRA-DAO", "Trà đào", menuTea, 39000);
        MenuItem("TRA-SUA", "Trà sữa trân châu", menuTea, 35000);
        MenuItem("TOP-TC", "Thêm trân châu", menuTopping, 7000);
        MenuItem("TOP-SHOT", "Thêm 1 shot cà phê", menuTopping, 10000);
        MenuItem("TOP-DAO", "Thêm đào", menuTopping, 8000);
        // Sold by the bottle: stock is tracked directly, no recipe.
        Add("NS-500", "Nước suối 500ml", ProductKind.FinishedGood, menuOther, "chai", 12000, 0, true);
        return catalog;
    }

    /// <summary>Marks toppings and declares recipes (quantities in the ingredient's base unit).</summary>
    private const int ToppingSortOrder = 99;

    private static void SeedMenu(AppDbContext db, SeededCatalog catalog)
    {
        db.AddRange(new[] { "TOP-TC", "TOP-SHOT", "TOP-DAO" }
            .Select(code => CafeItem.Topping(catalog[code].Id, ToppingSortOrder)));

        void Recipe(string menuCode, params (string IngredientCode, decimal Quantity)[] lines) =>
            db.AddRange(lines.Select(l => new RecipeLine
            {
                MenuProductId = catalog[menuCode].Id, IngredientProductId = catalog[l.IngredientCode].Id, Quantity = l.Quantity,
            }));

        Recipe("CF-DEN", ("NL-CF-ROB", 20), ("NL-DUONG", 10), ("BB-LY-500", 1));
        Recipe("CF-SUA", ("NL-CF-ROB", 20), ("NL-SUA-DAC", 30), ("BB-LY-500", 1));
        Recipe("CF-BACXIU", ("NL-CF-ROB", 10), ("NL-SUA-DAC", 40), ("NL-SUA-TUOI", 50), ("BB-LY-500", 1));
        Recipe("CF-LATTE", ("NL-CF-ARA", 18), ("NL-SUA-TUOI", 150));
        Recipe("TRA-DAO", ("NL-TRA-DEN", 5), ("NL-SYRUP-DAO", 30), ("NL-DAO-NGAM", 50), ("NL-DUONG", 10), ("BB-LY-500", 1));
        Recipe("TRA-SUA", ("NL-TRA-DEN", 6), ("NL-KEM-BEO", 30), ("NL-SUA-DAC", 20), ("NL-TRAN-CHAU", 40), ("BB-LY-500", 1));
        Recipe("TOP-TC", ("NL-TRAN-CHAU", 30));
        Recipe("TOP-SHOT", ("NL-CF-ROB", 10));
        Recipe("TOP-DAO", ("NL-DAO-NGAM", 40));
    }

    private static Warehouse[] SeedFloorPlansAndWarehouses(AppDbContext db, Branch[] branches)
    {
        var warehouses = new List<Warehouse>();
        foreach (var branch in branches)
        {
            var groundFloor = new Area { BranchId = branch.Id, Name = "Tầng trệt", SortOrder = 1 };
            var garden = new Area { BranchId = branch.Id, Name = "Sân vườn", SortOrder = 2 };
            db.AddRange(groundFloor, garden);
            for (var i = 1; i <= 8; i++) db.Add(new DiningTable { BranchId = branch.Id, AreaId = groundFloor.Id, Name = $"Bàn {i}", SortOrder = i });
            for (var i = 1; i <= 4; i++) db.Add(new DiningTable { BranchId = branch.Id, AreaId = garden.Id, Name = $"SV {i}", SortOrder = i });

            var warehouse = new Warehouse
            {
                BranchId = branch.Id, Code = $"KHO-{branch.Code}", Name = $"Kho {branch.Name.Replace("Cenvo ", "")}", IsDefault = true,
            };
            db.Add(warehouse);
            warehouses.Add(warehouse);
        }
        return [.. warehouses];
    }

    /// <summary>Opening stock goes through a stock document so the ledger and cost are recorded properly.</summary>
    private static async Task SeedOpeningStock(StockDocumentService documents, Guid warehouseId, SeededCatalog catalog, CancellationToken ct)
    {
        StockDocumentLineInput Line(string code, string? unit, decimal quantity, decimal unitPrice) =>
            new(catalog[code].Id, unit == null ? null : catalog.Unit(unit), quantity, unitPrice, null);

        var id = await documents.CreateDraft(new StockDocumentInput(
            StockDocumentType.OpeningBalance, warehouseId, null, null, null, "Tồn đầu kỳ",
            [
                Line("NL-CF-ROB", "kg", 10, 250000),
                Line("NL-CF-ARA", "kg", 3, 420000),
                Line("NL-TRA-DEN", "gói", 4, 90000),
                Line("NL-SUA-DAC", "hộp", 48, 26000),
                Line("NL-SUA-TUOI", "lít", 20, 32000),
                Line("NL-KEM-BEO", "lít", 5, 85000),
                Line("NL-DUONG", "kg", 10, 22000),
                Line("NL-SYRUP-DAO", "chai", 4, 165000),
                Line("NL-DAO-NGAM", "hộp", 6, 55000),
                Line("NL-TRAN-CHAU", "kg", 5, 60000),
                Line("BB-LY-500", "thùng", 1, 900000),
                Line("NS-500", null, 48, 5000),
            ]), ct);
        await documents.Post(id, ct);
    }
}
