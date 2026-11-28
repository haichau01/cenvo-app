using Microsoft.AspNetCore.Authorization;

namespace Cenvo.Api.Auth;

/// <summary>Role-based authorization policies — named so endpoints read like business rules.</summary>
public static class Policies
{
    public const string Admin = nameof(Admin);
    public const string Managers = nameof(Managers);
    public const string Cashiers = nameof(Cashiers);
    public const string Storekeepers = nameof(Storekeepers);
    /// <summary>Car wash floor: intake, queue, photos (washers plus the cashier/manager at the counter).</summary>
    public const string WashStaff = nameof(WashStaff);

    public static void AddCenvoPolicies(this AuthorizationOptions o)
    {
        o.AddPolicy(Admin, p => p.RequireRole(Roles.Admin));
        o.AddPolicy(Managers, p => p.RequireRole(Roles.Admin, Roles.BranchManager));
        o.AddPolicy(Cashiers, p => p.RequireRole(Roles.Admin, Roles.BranchManager, Roles.Cashier));
        o.AddPolicy(Storekeepers, p => p.RequireRole(Roles.Admin, Roles.BranchManager, Roles.Storekeeper));
        o.AddPolicy(WashStaff, p => p.RequireRole(Roles.Admin, Roles.BranchManager, Roles.Cashier, Roles.Washer));
    }
}
