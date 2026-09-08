using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Constants;

public static class TenantRoleSetupCatalog
{
    public const string TenantWideScope = "TENANT_WIDE";
    public const string SelectedOutletsScope = "SELECTED_OUTLETS";

    public static readonly TenantSetupRoleOption TenantAdmin = new(
        TenantUserConstants.DefaultTenantAdminRoleCode,
        "Tenant Admin",
        "Manage tenant administration, users, roles, products, outlets, tills, inventory, reports, and settings.");

    public static readonly TenantSetupRoleOption Cashier = new(
        TenantUserConstants.DefaultCashierRoleCode,
        "Cashier",
        "Access POS operations, tills, checkout, customers, and Release 1 cash-only payment flow.");

    public static IReadOnlyList<TenantSetupRoleOption> SupportedRoles { get; } =
    [
        TenantAdmin,
        Cashier
    ];

    public static IReadOnlySet<string> SupportedRoleCodes { get; } = SupportedRoles
        .Select(static role => role.RoleCode)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> LegacyCashierAllowedPermissionCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            PosPermissions.Home.View,
            PosPermissions.Home.ViewDashboard,
            PosPermissions.NewSale.View,
            PosPermissions.Notifications.View,
            PosPermissions.Hardware.Settings,
            PosPermissions.Till.Open,
            PosPermissions.Till.Close,
            PosPermissions.Till.ViewSession,
            TillConstants.ManagePermission,
            "tenant.till.manage",
            SalesPermissions.Sale.Create,
            SalesPermissions.Sale.View,
            SalesPermissions.Sale.Checkout,
            ProductPosPermissions.View,
            ProductPosPermissions.Search,
            SalesPermissions.Cart.Manage,
            SalesPermissions.Cart.AddItem,
            SalesPermissions.Cart.UpdateItem,
            SalesPermissions.Cart.RemoveItem,
            SalesPermissions.Cart.Clear,
            SalesPermissions.Discount.Apply,
            SalesPermissions.Park.Create,
            SalesPermissions.Park.View,
            SalesPermissions.Park.Recall,
            SalesPermissions.Orders.View,
            PaymentPermissions.AcceptCash,
            PaymentPermissions.AcceptCard,
            PaymentPermissions.AcceptQr,
            PaymentPermissions.AcceptSplit,
            ReceiptPermissions.View,
            ReceiptPermissions.Print,
            ReceiptPermissions.Reprint,
            ReturnsPermissions.ViewReturns,
            ReturnsPermissions.CreateReturn,
            ReturnsPermissions.ViewRefunds,
            ReturnsPermissions.CreateRefund,
            ReturnsPermissions.ViewExchanges,
            ReturnsPermissions.CreateExchange,
            CustomerPermissions.View,
            CustomerPermissions.Create,
            CustomerPermissions.Update,
            CustomerPermissions.AttachSale,
            CustomerPermissions.Deactivate,
            CashDrawerPermissions.View,
            CashDrawerPermissions.Manage,
            CashDrawerPermissions.CreateMovement,
            CashDrawerPermissions.Canonical.PositionView,
            CashDrawerPermissions.Canonical.PhysicalManage,
            CashDrawerPermissions.Canonical.MovementsCreate,
            CashDrawerPermissions.Canonical.CashIn,
            CashDrawerPermissions.Canonical.CashOut,
            CashDrawerPermissions.Canonical.CashDrop,
            SalesPermissions.HeldSales.Create,
            SalesPermissions.HeldSales.View,
            SalesPermissions.HeldSales.Recall,
            SalesPermissions.HeldSales.Cancel,
        };

    /// <summary>
    /// Cashier ceiling: legacy aliases + all Chunk 2/3 role-assignable Cashier POS codes.
    /// Ongoing assignment still validates format/pre-auth/parent independently.
    /// </summary>
    public static IReadOnlySet<string> CashierAllowedPermissionCodes { get; } =
        LegacyCashierAllowedPermissionCodes
            .Concat(OnlineOrderPickingPermissions.All)
            .Concat(CashierPosPermissionAssignmentRules.RoleAssignablePermissionCodes)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlySet<string> AdministrativePermissionCodes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            TenantAdminUserPermissions.Manage,
            TenantAdminUserPermissions.Create,
            TenantAdminUserPermissions.Update,
            TenantAdminUserPermissions.Delete,
            TenantAdminUserPermissions.RolesManage,
            TenantAdminUserPermissions.RolesCreate,
            TenantAdminUserPermissions.RolesUpdate,
            TenantAdminUserPermissions.RolesDelete,
            TenantAdminUserPermissions.RolesPermissionsUpdate,
            TenantAdminUserPermissions.RolesAssignmentsUpdate
        };

    public static bool IsSupportedSetupRoleCode(string? roleCode) =>
        !string.IsNullOrWhiteSpace(roleCode) &&
        SupportedRoleCodes.Contains(roleCode.Trim());

    public static bool IsCashierRoleCode(string? roleCode) =>
        string.Equals(roleCode?.Trim(), TenantUserConstants.DefaultCashierRoleCode, StringComparison.OrdinalIgnoreCase);

    public static bool IsTenantAdminRoleCode(string? roleCode) =>
        string.Equals(roleCode?.Trim(), TenantUserConstants.DefaultTenantAdminRoleCode, StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedForRole(string roleCode, string permissionCode)
    {
        if (IsCashierRoleCode(roleCode))
        {
            return CashierAllowedPermissionCodes.Contains(permissionCode);
        }

        return true;
    }
}

public sealed record TenantSetupRoleOption(
    string RoleCode,
    string RoleName,
    string RoleDescription);
