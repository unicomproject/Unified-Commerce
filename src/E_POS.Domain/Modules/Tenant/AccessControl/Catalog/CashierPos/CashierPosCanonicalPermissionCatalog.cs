#nullable enable
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos.Modules;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

public static class CashierPosCanonicalPermissionCatalog
{
    public static IReadOnlyList<CashierPosPermissionDefinition> All { get; } =
    [
        .. PreAuthPermissions.All,
        .. ShellPermissions.All,
        .. NotificationPermissions.All,
        .. HomePermissions.All,
        .. CatalogPermissions.All,
        .. CartPermissions.All,
        .. HeldSalesPermissions.All,
        .. CheckoutPermissions.All,
        .. DiscountPermissions.All,
        .. CashPaymentPermissions.All,
        .. SaleCompletePermissions.All,
        .. ReceiptPermissions.All,
        .. CustomerPermissions.All,
        .. CashDrawerPermissions.All,
        .. CashMovementPermissions.All,
        .. TillPermissions.All,
        .. ReturnPermissions.All,
        .. HardwarePermissions.All,
        .. OnlineOrderPermissions.All,
    ];

    public static IEnumerable<CashierPosPermissionDefinition> RoleAssignable =>
        All.Where(d => d.IsRoleAssignable);

    public static IEnumerable<CashierPosPermissionDefinition> FineGrained =>
        All.Where(d => d.Kind is CashierPosPermissionDefinitionKind.New
            or CashierPosPermissionDefinitionKind.Split
            or CashierPosPermissionDefinitionKind.DocumentedResolved);
}
