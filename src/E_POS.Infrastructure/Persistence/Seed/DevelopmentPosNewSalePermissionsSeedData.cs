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
        new(Guid.Parse("77777777-0301-4000-8000-000000000001"), PosPermissions.Home.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosHomeFeatureId, "view", "View POS home dashboard and sidebar."),
        new(Guid.Parse("77777777-0332-4000-8000-000000000001"), PosPermissions.Home.ViewDashboard, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosHomeFeatureId, "view_dashboard", "View POS cashier dashboard."),
        new(Guid.Parse("77777777-0302-4000-8000-000000000001"), PosPermissions.NewSale.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "view", "View POS new sale route and actions."),
        new(Guid.Parse("77777777-0303-4000-8000-000000000001"), SalesPermissions.Sale.Create, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "create", "Create a new POS sale."),
        new(Guid.Parse("77777777-0304-4000-8000-000000000001"), ProductPosPermissions.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosProductsFeatureId, "view", "View products on POS new sale."),
        new(Guid.Parse("77777777-0305-4000-8000-000000000001"), ProductPosPermissions.Search, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosProductsFeatureId, "search", "Search products on POS new sale."),
        new(Guid.Parse("77777777-0306-4000-8000-000000000001"), SalesPermissions.Cart.Manage, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "manage", "Manage POS sale cart."),
        new(Guid.Parse("77777777-0307-4000-8000-000000000001"), SalesPermissions.Cart.AddItem, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "add_item", "Add items to POS sale cart."),
        new(Guid.Parse("77777777-0308-4000-8000-000000000001"), SalesPermissions.Cart.UpdateItem, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "update_item", "Update items in POS sale cart."),
        new(Guid.Parse("77777777-0309-4000-8000-000000000001"), SalesPermissions.Cart.RemoveItem, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "remove_item", "Remove items from POS sale cart."),
        new(Guid.Parse("77777777-0310-4000-8000-000000000001"), SalesPermissions.Cart.Clear, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "clear", "Clear POS sale cart."),
        new(Guid.Parse("77777777-0311-4000-8000-000000000001"), CustomerPermissions.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCustomersFeatureId, "view", "View customers on POS."),
        new(Guid.Parse("77777777-0312-4000-8000-000000000001"), CustomerPermissions.Create, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCustomersFeatureId, "create", "Create customers on POS."),
        new(Guid.Parse("77777777-0313-4000-8000-000000000001"), SalesPermissions.Discount.Apply, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "apply", "Apply discounts on POS sale."),
        new(Guid.Parse("77777777-0314-4000-8000-000000000001"), SalesPermissions.Park.Create, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "park_create", "Park a POS sale for later."),
        new(Guid.Parse("77777777-0315-4000-8000-000000000001"), SalesPermissions.Park.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "park_view", "View parked POS sales."),
        new(Guid.Parse("77777777-0333-4000-8000-000000000001"), SalesPermissions.Park.Recall, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId, "park_recall", "Recall parked POS sales."),
    ];

    public static IReadOnlyList<string> CashierPermissionCodes { get; } =
        Definitions.Select(static definition => definition.PermissionCode).ToList();

    public static string UpSql => TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(Definitions);

    public static string DownSql => TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(Definitions);
}
