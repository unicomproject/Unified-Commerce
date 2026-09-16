namespace E_POS.Application.Modules.Tenant.AccessControl.Mappers;

/// <summary>
/// Maps technical permission namespaces to the feature modules shown in the
/// tenant role setup UI. Commercial platform modules remain unchanged because
/// they are also used for subscription and entitlement evaluation.
/// </summary>
public static class TenantPermissionPresentationModuleCatalog
{
    private static readonly TenantPermissionPresentationModule Dashboard = new(
        Guid.Parse("73500000-0000-0000-0000-000000000001"),
        "dashboard",
        "Dashboard",
        "Business overview and operational insights.",
        1);

    private static readonly TenantPermissionPresentationModule Outlets = new(
        Guid.Parse("73500000-0000-0000-0000-000000000002"),
        "outlets",
        "Outlets",
        "Manage outlet information and operations.",
        2);

    private static readonly TenantPermissionPresentationModule Tills = new(
        Guid.Parse("73500000-0000-0000-0000-000000000003"),
        "tills",
        "Tills",
        "Till configuration, devices and hardware monitoring.",
        3);

    private static readonly TenantPermissionPresentationModule Users = new(
        Guid.Parse("73500000-0000-0000-0000-000000000004"),
        "users",
        "Users",
        "User accounts, invitations and access scope.",
        4);

    private static readonly TenantPermissionPresentationModule RolesAccess = new(
        Guid.Parse("73500000-0000-0000-0000-000000000005"),
        "roles-access",
        "Roles & Access",
        "Roles, permissions and access assignments.",
        5);

    private static readonly TenantPermissionPresentationModule Products = new(
        Guid.Parse("73500000-0000-0000-0000-000000000006"),
        "products",
        "Products",
        "Products, catalog, pricing and tax configuration.",
        6);

    private static readonly TenantPermissionPresentationModule Inventory = new(
        Guid.Parse("73500000-0000-0000-0000-000000000007"),
        "inventory",
        "Inventory",
        "Stock management and inventory operations.",
        7);

    private static readonly TenantPermissionPresentationModule SalesPos = new(
        Guid.Parse("73500000-0000-0000-0000-000000000008"),
        "sales_pos",
        "Sales (POS)",
        "Process sales, payments, orders and returns.",
        8);

    private static readonly TenantPermissionPresentationModule Reports = new(
        Guid.Parse("73500000-0000-0000-0000-000000000009"),
        "reports",
        "Reports",
        "Business and operational reports.",
        9);

    private static readonly TenantPermissionPresentationModule OnlineStore = new(
        Guid.Parse("73500000-0000-0000-0000-000000000010"),
        "online-store",
        "Online Store",
        "Online storefront and click & collect operations.",
        10);

    private static readonly TenantPermissionPresentationModule Settings = new(
        Guid.Parse("73500000-0000-0000-0000-000000000011"),
        "settings",
        "Settings",
        "Workspace and tenant configuration.",
        11);

    public static TenantPermissionPresentationModule Resolve(string permissionCode)
    {
        var code = permissionCode.Trim().ToLowerInvariant();

        if (StartsWithAny(
                code,
                "tenant.dashboard.",
                "dashboard.",
                "pos.dashboard.",
                "pos.home.",
                "notifications."))
            return Dashboard;

        if (StartsWithAny(code, "tenant.outlets.", "outlets.", "outlet."))
            return Outlets;

        if (StartsWithAny(
                code,
                "tenant.tills.",
                "tenant.till.",
                "tenant.devices.",
                "tenant.hardware.",
                "hardware.",
                "tills.",
                "till.",
                "till.session.",
                "devices."))
            return Tills;

        if (StartsWithAny(code, "tenant.users.", "users.", "tenant_user."))
            return Users;

        if (StartsWithAny(code, "tenant.roles.", "tenant.permissions.", "roles.", "permissions."))
            return RolesAccess;

        if (StartsWithAny(
                code,
                "tenant.products.",
                "catalog.",
                "products.",
                "product.",
                "pricing.",
                "tax.",
                "discount.policy."))
            return Products;

        if (StartsWithAny(code, "tenant.stock.", "inventory.", "stock."))
            return Inventory;

        if (StartsWithAny(code, "tenant.reports.", "reports.", "report."))
            return Reports;

        if (StartsWithAny(
                code,
                "tenant.online_store.",
                "tenant.online-store.",
                "online_store.",
                "online-store.",
                "ecommerce.",
                "storefront.",
                "click_collect.",
                "click-collect.",
                "commerce.online_order.",
                "fulfillment.orders."))
            return OnlineStore;

        if (StartsWithAny(
                code,
                "pos.",
                "sales.",
                "orders.",
                "payments.",
                "payment.",
                "receipts.",
                "returns.",
                "refunds.",
                "exchanges.",
                "customers.",
                "cash_drawer.",
                "sales_order.",
                "workspace.pos."))
            return SalesPos;

        return Settings;
    }

    private static bool StartsWithAny(string value, params string[] prefixes) =>
        prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
}

public sealed record TenantPermissionPresentationModule(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int SortOrder);
