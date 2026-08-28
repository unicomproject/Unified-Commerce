namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Canonical POS click-and-collect permissions used by the cashier online-order journey.
/// </summary>
public static class DevelopmentPosOnlineOrderPermissionsSeedData
{
    public const string Access = "commerce.online_order.orders.access";
    public const string View = "commerce.online_order.orders.view";
    public const string StartFulfillment = "commerce.online_order.fulfilment.start";
    public const string PickingView = "commerce.online_order.picking.view";
    public const string PickingPick = "commerce.online_order.picking.pick";
    public const string PickingScan = "commerce.online_order.picking.scan";
    public const string PickingManualEntry = "commerce.online_order.picking.manual_entry";
    public const string PickingReportIssue = "commerce.online_order.picking.report_issue";
    public const string PackingView = "commerce.online_order.packing.view";
    public const string PackingPack = "commerce.online_order.packing.pack";
    public const string CollectionMarkReady = "commerce.online_order.collection.mark_ready";

    public static IReadOnlyList<string> PermissionCodes { get; } =
        [Access, View, StartFulfillment, PickingView, PickingPick, PickingScan,
            PickingManualEntry, PickingReportIssue, PackingView, PackingPack,
            CollectionMarkReady];

    public static IReadOnlyList<TenantPermissionSeedDefinition> Definitions { get; } =
    [
        new(
            Guid.Parse("77777777-0370-4000-8000-000000000001"),
            Access,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId,
            "online_orders_access",
            "Access the cashier click-and-collect online-order workspace."),
        new(
            Guid.Parse("77777777-0371-4000-8000-000000000001"),
            View,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId,
            "online_orders_view",
            "View outlet-scoped click-and-collect orders and fulfilment details."),
        new(
            Guid.Parse("77777777-0372-4000-8000-000000000001"),
            StartFulfillment,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId,
            "online_order_fulfilment_start",
            "Start fulfilment and assign an online order for picking."),
        new(
            Guid.Parse("77777777-0373-4000-8000-000000000001"),
            PickingView,
            DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
            DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId,
            "online_order_picking_view",
            "View the picking workspace for an assigned online order."),
        new(Guid.Parse("77777777-0374-4000-8000-000000000001"), PickingPick, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_picking_pick", "Confirm picked quantities for online orders."),
        new(Guid.Parse("77777777-0375-4000-8000-000000000001"), PickingScan, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_picking_scan", "Verify online-order items using a barcode scanner."),
        new(Guid.Parse("77777777-0376-4000-8000-000000000001"), PickingManualEntry, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_picking_manual", "Verify online-order items using manual barcode entry."),
        new(Guid.Parse("77777777-0377-4000-8000-000000000001"), PickingReportIssue, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_picking_issue", "Report an unresolved online-order picking issue."),
        new(Guid.Parse("77777777-0378-4000-8000-000000000001"), PackingView, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_packing_view", "View online-order packing review."),
        new(Guid.Parse("77777777-0379-4000-8000-000000000001"), PackingPack, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_packing_pack", "Create packages for fully picked online orders."),
        new(Guid.Parse("77777777-0380-4000-8000-000000000001"), CollectionMarkReady, DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId, DevelopmentPosPermissionCatalogSeedConstants.PosOnlineOrdersFeatureId, "online_order_collection_ready", "Mark packed online orders ready for collection.")
    ];

    public static string UpSql => TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(Definitions);
    public static string DownSql => TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(Definitions);
    public static string CashierAssignmentUpSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql(PermissionCodes);
    public static string CashierAssignmentDownSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentDeleteSql(PermissionCodes);
}
