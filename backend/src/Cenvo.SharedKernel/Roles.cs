namespace Cenvo.SharedKernel;

public static class Roles
{
    public const string Admin = "Admin";
    public const string BranchManager = "BranchManager";
    public const string Cashier = "Cashier";
    public const string Barista = "Barista";
    public const string Storekeeper = "Storekeeper";
    public static readonly string[] All = [Admin, BranchManager, Cashier, Barista, Storekeeper];

    /// <summary>May perform actions that require manager approval (cancel a paid order, remove an item being prepared...).</summary>
    public static bool IsManager(this ICurrentUser user) => user.IsInRole(Admin) || user.IsInRole(BranchManager);
}
