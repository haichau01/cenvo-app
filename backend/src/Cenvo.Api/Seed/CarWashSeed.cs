using Cenvo.Infrastructure.Persistence;
using Cenvo.Modules.CarWash.Domain;
using Cenvo.Modules.Catalog.Domain;
using Cenvo.Modules.Inventory.Application.Documents;
using Cenvo.Modules.Inventory.Domain;
using Cenvo.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cenvo.Api.Seed;

/// <summary>
/// Car wash sample data: supplies + opening stock, washers, price list (per vehicle type, supplies per vehicle), one package.
/// Runs once — skipped when any car wash service exists — so it also fills databases seeded before the module existed.
/// </summary>
internal static class CarWashSeed
{
    private const int MembershipValidDays = 180;
    private const string SupplyUnit = "ml";

    private static readonly (string Code, string Name, decimal MinStock, string PackUnit, decimal PackMl, decimal Packs, decimal PackCost)[]
        Supplies =
        [
            ("HC-BOT", "Dung dịch bọt tuyết", 5000, "can", 20000, 2, 1200000),
            ("HC-RUA", "Nước rửa xe đậm đặc", 5000, "can", 20000, 2, 650000),
            ("HC-WAX", "Wax bóng sơn", 1000, "chai", 1000, 6, 280000),
            ("HC-NOITHAT", "Dung dịch vệ sinh nội thất", 1000, "chai", 1000, 6, 180000),
        ];

    private static readonly (string UserName, string FullName, string BranchCode)[] Washers =
    [
        ("ruaxe1", "Ngô Văn Rửa", "CN01"),
        ("ruaxe2", "Bùi Văn Sạch", "CN01"),
        ("ruaxe3", "Lý Minh Bóng", "CN02"),
    ];

    public static async Task Run(IServiceProvider sp, AppDbContext db, CurrentUser currentUser, CancellationToken ct)
    {
        if (await db.Set<WashOffering>().IgnoreQueryFilters().AnyAsync(ct)) return; // seed once, even if all were deleted
        var branches = await db.Set<Branch>().ToDictionaryAsync(b => b.Code, b => b.Id, ct);
        if (branches.Count == 0) return;

        var products = await SeedSupplies(db, ct);
        await SeedWashers(db, branches, ct);
        SeedPriceList(db, products);
        await db.SaveChangesAsync(ct);

        var documents = sp.GetRequiredService<StockDocumentService>();
        foreach (var warehouse in await db.Set<Warehouse>().Where(w => w.IsDefault).ToListAsync(ct))
        {
            currentUser.RunAs(SystemUser.ForBranch(warehouse.BranchId));
            await SeedOpeningStock(documents, warehouse.Id, products, db, ct);
        }
    }

    private static async Task<Dictionary<string, Product>> SeedSupplies(AppDbContext db, CancellationToken ct)
    {
        var units = await db.Set<Unit>().ToDictionaryAsync(u => u.Name, ct);
        Unit UnitNamed(string name) => units.TryGetValue(name, out var unit) ? unit : units[name] = db.Add(new Unit { Name = name }).Entity;

        var category = db.Add(new Category { Name = "Hóa chất rửa xe", Kind = ProductKind.Supply, SortOrder = 5 }).Entity;
        var existing = await db.Set<Product>().Where(p => Supplies.Select(s => s.Code).Contains(p.Code)).ToDictionaryAsync(p => p.Code, ct);
        foreach (var supply in Supplies.Where(s => !existing.ContainsKey(s.Code)))
        {
            var product = new Product
            {
                Code = supply.Code, Name = supply.Name, Kind = ProductKind.Supply, CategoryId = category.Id,
                BaseUnitId = UnitNamed(SupplyUnit).Id, MinStock = supply.MinStock, TrackStock = true,
            };
            product.SetUnits([(UnitNamed(supply.PackUnit).Id, supply.PackMl)]);
            existing[supply.Code] = db.Add(product).Entity;
        }
        return existing;
    }

    private static async Task SeedWashers(AppDbContext db, Dictionary<string, Guid> branches, CancellationToken ct)
    {
        var taken = await db.Set<AppUser>().IgnoreQueryFilters() // a deleted account still owns its user name
            .Select(u => u.UserName).ToListAsync(ct);
        foreach (var washer in Washers.Where(w => !taken.Contains(w.UserName) && branches.ContainsKey(w.BranchCode)))
        {
            var user = new AppUser { UserName = washer.UserName, FullName = washer.FullName };
            user.SetPassword(DataSeeder.DefaultPassword);
            user.SetRoles([Roles.Washer]);
            user.SetBranches([branches[washer.BranchCode]]);
            db.Add(user);
        }
    }

    private static void SeedPriceList(AppDbContext db, IReadOnlyDictionary<string, Product> products)
    {
        WashOffering Offering(string code, string name, int minutes, CommissionType commissionType, decimal commission,
            int sortOrder, (decimal Sedan, decimal SevenSeater, decimal Pickup, decimal Suv) prices,
            params (string ProductCode, decimal Ml)[] supplies)
        {
            var offering = new WashOffering();
            offering.Update(code, name, minutes, commissionType, commission, sortOrder, isActive: true);
            offering.SetPrices([
                (VehicleType.Sedan, prices.Sedan), (VehicleType.SevenSeater, prices.SevenSeater),
                (VehicleType.Pickup, prices.Pickup), (VehicleType.Suv, prices.Suv),
            ]);
            offering.SetSupplies(supplies.Select(s => (products[s.ProductCode].Id, (Guid?)null, s.Ml)));
            db.Add(offering);
            return offering;
        }

        Offering("RX-THUONG", "Rửa thường", 20, CommissionType.Percent, 20, 1, (60000, 70000, 80000, 80000), ("HC-RUA", 50));
        var foam = Offering("RX-BOT", "Rửa bọt tuyết", 30, CommissionType.Percent, 20, 2, (100000, 120000, 130000, 130000),
            ("HC-BOT", 150), ("HC-RUA", 30));
        Offering("HUT-BUI", "Hút bụi", 15, CommissionType.FixedAmount, 10000, 3, (40000, 50000, 50000, 50000));
        Offering("VS-NOITHAT", "Vệ sinh nội thất", 60, CommissionType.Percent, 15, 4, (350000, 450000, 450000, 500000),
            ("HC-NOITHAT", 200));
        Offering("DANH-BONG", "Đánh bóng & wax", 90, CommissionType.FixedAmount, 50000, 5, (450000, 550000, 600000, 600000),
            ("HC-WAX", 80));

        var package = new MembershipPackage();
        package.Update("GOI-BOT-10", "Rửa bọt tuyết — mua 10 tặng 2", foam.Id, VehicleType.Sedan, price: 1000000, uses: 12,
            MembershipValidDays, isActive: true);
        db.Add(package);
    }

    /// <summary>Goes through a stock document so the ledger and cost are recorded properly.</summary>
    private static async Task SeedOpeningStock(StockDocumentService documents, Guid warehouseId,
        IReadOnlyDictionary<string, Product> products, AppDbContext db, CancellationToken ct)
    {
        var units = await db.Set<Unit>().ToDictionaryAsync(u => u.Name, u => u.Id, ct);
        var id = await documents.CreateDraft(new StockDocumentInput(
            StockDocumentType.OpeningBalance, warehouseId, null, null, null, "Tồn đầu kỳ hóa chất rửa xe",
            Supplies.Select(s => new StockDocumentLineInput(products[s.Code].Id, units[s.PackUnit], s.Packs, s.PackCost, null)).ToArray()),
            ct);
        await documents.Post(id, ct);
    }
}
