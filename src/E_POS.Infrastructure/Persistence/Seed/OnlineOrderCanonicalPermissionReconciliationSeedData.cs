using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Reconciles frozen Permission_Code_List commerce Online Order codes into
/// permission_definitions and backfills grants from legacy pos.online_orders.* /
/// fulfillment.orders.* / feature-access pair — tenant-scoped, active-only,
/// no cross-tenant, no duplicates. Does not invent new permission codes.
/// </summary>
public static class OnlineOrderCanonicalPermissionReconciliationSeedData
{
    public const string RoleNotes = "Online Order legacy→canonical grant reconciliation.";
    public const string CompatRoleIdPrefix = "oo-canon-role:";
    public const string CompatUserIdPrefix = "oo-canon-user:";

    /// <summary>
    /// Upsert all current Chunk 3 role-assignable definitions (idempotent),
    /// including newly reconciled commerce.online_order.* Existing entries.
    /// </summary>
    public static string DefinitionUpsertSql => CashierPosChunk3PermissionSeedData.DefinitionUpsertSql;

    public static string LegacyGrantBackfillSql { get; } = BuildLegacyGrantBackfillSql();

    public static string UpSql =>
        DefinitionUpsertSql
        + Environment.NewLine
        + LegacyGrantBackfillSql;

    public static string DownSql => """
        DELETE FROM tenant_role_permissions
        WHERE notes = 'Online Order legacy→canonical grant reconciliation.';

        DELETE FROM tenant_user_permissions tup
        USING permission_definitions pd
        WHERE tup.permission_id = pd.id
          AND pd.permission_code LIKE 'commerce.online_order.%'
          AND pd.permission_code <> 'commerce.online_order.orders.access'
          AND tup.id = md5(
                'oo-canon-user:'
                || tup.tenant_id::text
                || ':'
                || tup.user_id::text
                || ':'
                || pd.permission_code)::uuid;
        """;

    private static string BuildLegacyGrantBackfillSql()
    {
        // legacy_code → canonical_code (Permission_Code_List + historical seed namespace)
        var pairs = new (string Legacy, string Canonical)[]
        {
            // Feature access pair: DetailService + Flutter route require both.
            (OnlineOrderPickingPermissions.OrdersAccess, OnlineOrderPickingPermissions.OrdersView),
            ("pos.online_orders.manage", OnlineOrderPickingPermissions.OrdersAccess),
            ("pos.online_orders.manage", OnlineOrderPickingPermissions.OrdersView),
            ("pos.online_orders.manage", OnlineOrderPickingPermissions.FulfilmentStart),
            ("fulfillment.orders.view", OnlineOrderPickingPermissions.OrdersView),
            ("fulfillment.orders.manage", OnlineOrderPickingPermissions.FulfilmentStart),
            // Legacy pos.online_orders.* seed namespace → canonical commerce.*
            ("pos.online_orders.picking.view", OnlineOrderPickingPermissions.PickingView),
            ("pos.online_orders.picking.view", OnlineOrderPickingPermissions.FulfilmentStart),
            ("pos.online_orders.picking.view", OnlineOrderPickingPermissions.PickingNote),
            ("pos.online_orders.picking.pick", OnlineOrderPickingPermissions.PickingPick),
            ("pos.online_orders.picking.scan", OnlineOrderPickingPermissions.PickingScan),
            ("pos.online_orders.picking.manual_entry", OnlineOrderPickingPermissions.PickingManualEntry),
            ("pos.online_orders.picking.report_issue", OnlineOrderPickingPermissions.PickingReportIssue),
            ("pos.online_orders.packing.view", OnlineOrderPickingPermissions.PackingView),
            ("pos.online_orders.packing.pack", OnlineOrderPickingPermissions.PackingPack),
            ("pos.online_orders.collection.mark_ready", OnlineOrderPickingPermissions.CollectionMarkReady),
        };

        var values = string.Join(",\n                ",
            pairs.Select(p => $"('{p.Legacy}', '{p.Canonical}')"));

        return $$"""
            WITH pairs(legacy_code, canonical_code) AS (
                VALUES
                {{values}}
            ),
            role_backfill AS (
                INSERT INTO tenant_role_permissions (
                    id,
                    tenant_id,
                    role_id,
                    permission_id,
                    granted_by_tenant_user_id,
                    granted_at,
                    notes,
                    created_at)
                SELECT
                    md5(
                        'oo-canon-role:'
                        || trp.tenant_id::text
                        || ':'
                        || trp.role_id::text
                        || ':'
                        || pairs.canonical_code)::uuid,
                    trp.tenant_id,
                    trp.role_id,
                    canonical_def.id,
                    NULL,
                    now(),
                    'Online Order legacy→canonical grant reconciliation.',
                    now()
                FROM pairs
                JOIN permission_definitions legacy_def
                    ON legacy_def.permission_code = pairs.legacy_code
                   AND legacy_def.is_active = TRUE
                JOIN permission_definitions canonical_def
                    ON canonical_def.permission_code = pairs.canonical_code
                   AND canonical_def.is_active = TRUE
                JOIN tenant_role_permissions trp
                    ON trp.permission_id = legacy_def.id
                   AND trp.revoked_at IS NULL
                ON CONFLICT DO NOTHING
                RETURNING 1
            ),
            user_backfill AS (
                INSERT INTO tenant_user_permissions (
                    id,
                    tenant_id,
                    user_id,
                    permission_id,
                    assigned_by_tenant_user_id,
                    assigned_at,
                    revoked_at,
                    created_at)
                SELECT
                    md5(
                        'oo-canon-user:'
                        || tup.tenant_id::text
                        || ':'
                        || tup.user_id::text
                        || ':'
                        || pairs.canonical_code)::uuid,
                    tup.tenant_id,
                    tup.user_id,
                    canonical_def.id,
                    NULL,
                    now(),
                    NULL,
                    now()
                FROM pairs
                JOIN permission_definitions legacy_def
                    ON legacy_def.permission_code = pairs.legacy_code
                   AND legacy_def.is_active = TRUE
                JOIN permission_definitions canonical_def
                    ON canonical_def.permission_code = pairs.canonical_code
                   AND canonical_def.is_active = TRUE
                JOIN tenant_user_permissions tup
                    ON tup.permission_id = legacy_def.id
                   AND tup.revoked_at IS NULL
                ON CONFLICT (tenant_id, user_id, permission_id) DO UPDATE
                SET revoked_at = NULL,
                    assigned_at = COALESCE(tenant_user_permissions.assigned_at, EXCLUDED.assigned_at)
                RETURNING 1
            )
            SELECT
                (SELECT count(*) FROM role_backfill) AS role_rows,
                (SELECT count(*) FROM user_backfill) AS user_rows;
            """;
    }
}
