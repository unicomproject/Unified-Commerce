using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Canonical idempotent seed for Cashier Online Order and Picking permissions.
/// Maps orders.access and orders.view to sales_orders feature,
/// and picking.* to click_collect feature.
/// </summary>
public static class DevelopmentPosCashierOnlineOrderPermissionsSeedData
{
    public static readonly Guid CoreCommerceModuleId =
        SubscriptionBillingCatalogSeedConstants.CoreCommerceModuleId;

    public static readonly Guid SalesOrdersFeatureId =
        Guid.Parse("72000000-0000-0000-0000-000000000004");

    public static readonly Guid ClickCollectFeatureId =
        SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId;

    public static readonly Guid OrdersAccessId =
        Guid.Parse("77777777-0360-4000-8000-000000000001");

    public static readonly Guid OrdersViewId =
        Guid.Parse("77777777-0361-4000-8000-000000000001");

    public static readonly Guid PickingViewId =
        Guid.Parse("77777777-0362-4000-8000-000000000001");

    public static readonly Guid PickingPickId =
        Guid.Parse("77777777-0363-4000-8000-000000000001");

    public static readonly Guid PickingScanId =
        Guid.Parse("77777777-0364-4000-8000-000000000001");

    public static readonly Guid PickingManualEntryId =
        Guid.Parse("77777777-0365-4000-8000-000000000001");

    public static readonly Guid PickingReportIssueId =
        Guid.Parse("77777777-0366-4000-8000-000000000001");

    public static readonly Guid PickingNoteId =
        Guid.Parse("77777777-0367-4000-8000-000000000001");

    public static IReadOnlyList<TenantPermissionSeedDefinition> Definitions { get; } =
    [
        new(OrdersAccessId, OnlineOrderPickingPermissions.OrdersAccess, CoreCommerceModuleId, SalesOrdersFeatureId, "access", "Access online orders module and queue."),
        new(OrdersViewId, OnlineOrderPickingPermissions.OrdersView, CoreCommerceModuleId, SalesOrdersFeatureId, "view", "View online order details."),
        new(PickingViewId, OnlineOrderPickingPermissions.PickingView, CoreCommerceModuleId, ClickCollectFeatureId, "view", "View online order picking queue and items."),
        new(PickingPickId, OnlineOrderPickingPermissions.PickingPick, CoreCommerceModuleId, ClickCollectFeatureId, "pick", "Pick items for online order fulfillment."),
        new(PickingScanId, OnlineOrderPickingPermissions.PickingScan, CoreCommerceModuleId, ClickCollectFeatureId, "scan", "Scan barcodes during online order picking."),
        new(PickingManualEntryId, OnlineOrderPickingPermissions.PickingManualEntry, CoreCommerceModuleId, ClickCollectFeatureId, "manual_entry", "Manually enter items or quantities during picking."),
        new(PickingReportIssueId, OnlineOrderPickingPermissions.PickingReportIssue, CoreCommerceModuleId, ClickCollectFeatureId, "report_issue", "Report issues during online order picking."),
        new(PickingNoteId, OnlineOrderPickingPermissions.PickingNote, CoreCommerceModuleId, ClickCollectFeatureId, "note", "Add notes to online order picking fulfillment.")
    ];

    public static IReadOnlyList<string> CashierPermissionCodes =>
        OnlineOrderPickingPermissions.All;

    public const string UpSql = """
        -- 1. Ensure sales_orders feature exists in platform_features under core_commerce
        INSERT INTO platform_features (
            id, platform_module_id, feature_code, feature_key, feature_name,
            is_core_feature, name, description, status, sort_order, scope, created_at, updated_at)
        VALUES (
            '72000000-0000-0000-0000-000000000004',
            '71000000-0000-0000-0000-000000000001',
            'sales_orders',
            'sales_orders',
            'Sales Orders',
            false,
            'Sales Orders',
            'Sales orders and lifecycle management.',
            'ACTIVE',
            4,
            'TENANT',
            now(),
            now()
        )
        ON CONFLICT (feature_key) DO UPDATE
        SET status = 'ACTIVE',
            updated_at = now();

        -- 2. Upsert the 8 cashier online order and picking permissions
        INSERT INTO permission_definitions (
            id, permission_code, module_id, feature_id, action_type, description, is_system, is_active, scope, created_at, updated_at)
        VALUES
            ('77777777-0360-4000-8000-000000000001', 'commerce.online_order.orders.access', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000004', 'access', 'Access online orders module and queue.', true, true, 'TENANT', now(), now()),
            ('77777777-0361-4000-8000-000000000001', 'commerce.online_order.orders.view', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000004', 'view', 'View online order details.', true, true, 'TENANT', now(), now()),
            ('77777777-0362-4000-8000-000000000001', 'commerce.online_order.picking.view', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000002', 'view', 'View online order picking queue and items.', true, true, 'TENANT', now(), now()),
            ('77777777-0363-4000-8000-000000000001', 'commerce.online_order.picking.pick', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000002', 'pick', 'Pick items for online order fulfillment.', true, true, 'TENANT', now(), now()),
            ('77777777-0364-4000-8000-000000000001', 'commerce.online_order.picking.scan', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000002', 'scan', 'Scan barcodes during online order picking.', true, true, 'TENANT', now(), now()),
            ('77777777-0365-4000-8000-000000000001', 'commerce.online_order.picking.manual_entry', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000002', 'manual_entry', 'Manually enter items or quantities during picking.', true, true, 'TENANT', now(), now()),
            ('77777777-0366-4000-8000-000000000001', 'commerce.online_order.picking.report_issue', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000002', 'report_issue', 'Report issues during online order picking.', true, true, 'TENANT', now(), now()),
            ('77777777-0367-4000-8000-000000000001', 'commerce.online_order.picking.note', '71000000-0000-0000-0000-000000000001', '72000000-0000-0000-0000-000000000002', 'note', 'Add notes to online order picking fulfillment.', true, true, 'TENANT', now(), now())
        ON CONFLICT (permission_code) DO UPDATE
        SET module_id = EXCLUDED.module_id,
            feature_id = EXCLUDED.feature_id,
            action_type = EXCLUDED.action_type,
            description = EXCLUDED.description,
            is_system = TRUE,
            is_active = TRUE,
            scope = 'TENANT',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM permission_definitions
        WHERE permission_code IN (
            'commerce.online_order.orders.access',
            'commerce.online_order.orders.view',
            'commerce.online_order.picking.view',
            'commerce.online_order.picking.pick',
            'commerce.online_order.picking.scan',
            'commerce.online_order.picking.manual_entry',
            'commerce.online_order.picking.report_issue',
            'commerce.online_order.picking.note'
        );
        """;
}
