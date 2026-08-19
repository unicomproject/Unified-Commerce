using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosNewSalePermissionsSeedData
{
    private static readonly Guid ModuleId = DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId;

    public static IReadOnlyList<TenantPermissionSeedDefinition> Definitions { get; } =
    [
        new(Guid.Parse("77777777-0301-4000-8000-000000000001"), PosPermissions.Home.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View POS home dashboard and sidebar."),
        new(Guid.Parse("77777777-0332-4000-8000-000000000001"), PosPermissions.Home.ViewDashboard, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view_dashboard", "View POS cashier dashboard."),
        new(Guid.Parse("77777777-0302-4000-8000-000000000001"), PosPermissions.NewSale.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View POS new sale route and actions."),
        new(Guid.Parse("77777777-0303-4000-8000-000000000001"), SalesPermissions.Sale.Create, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "create", "Create a new POS sale."),
        new(Guid.Parse("77777777-0304-4000-8000-000000000001"), ProductPosPermissions.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View products on POS new sale."),
        new(Guid.Parse("77777777-0305-4000-8000-000000000001"), ProductPosPermissions.Search, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "search", "Search products on POS new sale."),
        new(Guid.Parse("77777777-0306-4000-8000-000000000001"), SalesPermissions.Cart.Manage, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "manage", "Manage POS sale cart."),
        new(Guid.Parse("77777777-0307-4000-8000-000000000001"), SalesPermissions.Cart.AddItem, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "add_item", "Add items to POS sale cart."),
        new(Guid.Parse("77777777-0308-4000-8000-000000000001"), SalesPermissions.Cart.UpdateItem, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "update_item", "Update items in POS sale cart."),
        new(Guid.Parse("77777777-0309-4000-8000-000000000001"), SalesPermissions.Cart.RemoveItem, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "remove_item", "Remove items from POS sale cart."),
        new(Guid.Parse("77777777-0310-4000-8000-000000000001"), SalesPermissions.Cart.Clear, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "clear", "Clear POS sale cart."),
        new(Guid.Parse("77777777-0311-4000-8000-000000000001"), CustomerPermissions.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View customers on POS."),
        // Keep catalogue ID aligned with the historical definition and corrective migration.
        DevelopmentPosCustomerCreatePermissionSeedData.Definition,
        // Keep catalogue ID aligned with DevelopmentPosCustomerUpdatePermissionSeedData / migration.
        new(
            DevelopmentPosCustomerUpdatePermissionSeedData.PermissionId,
            CustomerPermissions.Update,
            ModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId,
            "update",
            "Update customers on POS."),
        new(Guid.Parse("77777777-0313-4000-8000-000000000001"), SalesPermissions.Discount.Apply, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "apply", "Apply discounts on POS sale."),
        new(Guid.Parse("77777777-0314-4000-8000-000000000001"), SalesPermissions.Park.Create, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "park_create", "Park a POS sale for later."),
        new(Guid.Parse("77777777-0315-4000-8000-000000000001"), SalesPermissions.Park.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "park_view", "View parked POS sales."),
        new(Guid.Parse("77777777-0333-4000-8000-000000000001"), SalesPermissions.Park.Recall, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "park_recall", "Recall parked POS sales."),
    ];

    public static IReadOnlyList<string> CashierPermissionCodes { get; } =
        Definitions.Select(static definition => definition.PermissionCode).ToList();

    public static string UpSql => TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(Definitions);

    public static string DownSql => TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(Definitions);
}
