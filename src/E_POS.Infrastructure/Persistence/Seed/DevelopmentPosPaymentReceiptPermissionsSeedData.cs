using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosPaymentReceiptPermissionsSeedData
{
    private static readonly Guid ModuleId = DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId;

    public static IReadOnlyList<TenantPermissionSeedDefinition> Definitions { get; } =
    [
        new(Guid.Parse("77777777-0316-4000-8000-000000000001"), SalesPermissions.Sale.Checkout, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "checkout", "Proceed to POS payment checkout."),
        new(Guid.Parse("77777777-0317-4000-8000-000000000001"), PaymentPermissions.AcceptCash, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "accept_cash", "Accept cash payments on POS."),
        new(Guid.Parse("77777777-0318-4000-8000-000000000001"), PaymentPermissions.AcceptCard, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "accept_card", "Accept card payments on POS."),
        new(Guid.Parse("77777777-0319-4000-8000-000000000001"), PaymentPermissions.AcceptQr, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "accept_qr", "Accept QR/mobile payments on POS."),
        new(Guid.Parse("77777777-0320-4000-8000-000000000001"), PaymentPermissions.AcceptSplit, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "accept_split", "Accept split payments on POS."),
        new(Guid.Parse("77777777-0321-4000-8000-000000000001"), SalesPermissions.Sale.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View completed POS sales."),
        new(Guid.Parse("77777777-0322-4000-8000-000000000001"), ReceiptPermissions.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View POS receipts."),
        new(Guid.Parse("77777777-0323-4000-8000-000000000001"), ReceiptPermissions.Print, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "print", "Print POS receipts."),
        new(Guid.Parse("a4d1b0c2-8f31-4a2b-9f76-50a8b98d9101"), ReceiptPermissions.Reprint, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "reprint", "Reprint persisted POS receipts with an audited reason."),
        new(Guid.Parse("77777777-0324-4000-8000-000000000001"), SalesPermissions.Orders.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View POS orders sidebar."),
        new(Guid.Parse("77777777-0325-4000-8000-000000000001"), ReturnsPermissions.ViewReturns, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view_returns", "View POS returns."),
        new(Guid.Parse("77777777-0326-4000-8000-000000000001"), ReturnsPermissions.ViewRefunds, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view_refunds", "View POS refunds."),
        new(Guid.Parse("77777777-0327-4000-8000-000000000001"), ReturnsPermissions.CreateRefund, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "create_refund", "Create POS refunds."),
        new(Guid.Parse("77777777-0328-4000-8000-000000000001"), CashDrawerPermissions.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View POS cash drawer."),
        new(Guid.Parse("77777777-0329-4000-8000-000000000001"), CashDrawerPermissions.Manage, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "manage", "Manage POS cash drawer actions."),
        new(Guid.Parse("77777777-0330-4000-8000-000000000001"), PosPermissions.Notifications.View, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view", "View POS notifications."),
        new(Guid.Parse("77777777-0331-4000-8000-000000000001"), PosPermissions.Till.ViewSession, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "view_session", "View till session status on POS home."),
        new(Guid.Parse("77777777-0334-4000-8000-000000000001"), PosPermissions.Till.Open, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "open", "Open a POS till session."),
        new(Guid.Parse("77777777-0012-4000-8000-000000000001"), PosPermissions.Till.Close, ModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosCheckoutFeatureId, "close", "Close the currently assigned open POS till session."),
        new(Guid.Parse("77777777-0335-4000-8000-000000000001"), TillConstants.ManagePermission, ModuleId, PlatformModuleCatalogPrerequisiteSeedConstants.TillManagementFeatureId, "manage", "Manage tenant tills for POS device activation."),
        new(Guid.Parse("77777777-0336-4000-8000-000000000001"), "tenant.till.manage", ModuleId, PlatformModuleCatalogPrerequisiteSeedConstants.TillManagementFeatureId, "manage_alias", "Frontend-compatible tenant till management permission."),
    ];

    public static IReadOnlyList<string> CashierPermissionCodes { get; } =
        Definitions.Select(static definition => definition.PermissionCode).ToList();

    public static string UpSql => TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(Definitions);

    public static string DownSql => TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(Definitions);
}
