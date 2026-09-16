using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPickupCollectionVerifyPermissions : Migration
    {
        // Defines commerce.online_order.collection.verify / .complete (the new QR
        // pickup-verification step) and grants them to exactly whoever already holds
        // commerce.online_order.collection.mark_ready — the existing permission that
        // gates marking an online order ready for collection. In this codebase that is
        // the cashier role, so this keeps pickup verification/completion scoped to the
        // same staff who already handle collection, without re-deriving the tenant
        // role/feature catalog this permission was originally granted through.
        public const string DefinePermissionsSql = """
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('commerce.online_order.collection.verify')::uuid,
                'commerce.online_order.collection.verify',
                mark_ready.module_id, mark_ready.feature_id, 'verify',
                'Verify a customer pickup code at collection.', true, true, 'TENANT', now(), now()
            FROM permission_definitions mark_ready
            WHERE mark_ready.permission_code = 'commerce.online_order.collection.mark_ready'
            ON CONFLICT (permission_code) DO NOTHING;

            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('commerce.online_order.collection.complete')::uuid,
                'commerce.online_order.collection.complete',
                mark_ready.module_id, mark_ready.feature_id, 'complete',
                'Mark a verified online order as collected by the customer.', true, true, 'TENANT', now(), now()
            FROM permission_definitions mark_ready
            WHERE mark_ready.permission_code = 'commerce.online_order.collection.mark_ready'
            ON CONFLICT (permission_code) DO NOTHING;
            """;

        public const string BackfillGrantsSql = """
            WITH pairs(canonical_code) AS (
                VALUES
                    ('commerce.online_order.collection.verify'),
                    ('commerce.online_order.collection.complete')
            ),
            role_backfill AS (
                INSERT INTO tenant_role_permissions (
                    id, tenant_id, role_id, permission_id,
                    granted_by_tenant_user_id, granted_at, notes, created_at)
                SELECT
                    md5(
                        'pickup-verify-role:' || trp.tenant_id::text || ':' ||
                        trp.role_id::text || ':' || pairs.canonical_code)::uuid,
                    trp.tenant_id, trp.role_id, canonical_def.id,
                    NULL, now(),
                    'Granted alongside commerce.online_order.collection.mark_ready for pickup QR verification.',
                    now()
                FROM pairs
                JOIN permission_definitions canonical_def
                    ON canonical_def.permission_code = pairs.canonical_code
                   AND canonical_def.is_active = TRUE
                JOIN permission_definitions mark_ready_def
                    ON mark_ready_def.permission_code = 'commerce.online_order.collection.mark_ready'
                   AND mark_ready_def.is_active = TRUE
                JOIN tenant_role_permissions trp
                    ON trp.permission_id = mark_ready_def.id
                   AND trp.revoked_at IS NULL
                ON CONFLICT DO NOTHING
                RETURNING 1
            ),
            user_backfill AS (
                INSERT INTO tenant_user_permissions (
                    id, tenant_id, user_id, permission_id,
                    assigned_by_tenant_user_id, assigned_at, revoked_at, created_at)
                SELECT
                    md5(
                        'pickup-verify-user:' || tup.tenant_id::text || ':' ||
                        tup.user_id::text || ':' || pairs.canonical_code)::uuid,
                    tup.tenant_id, tup.user_id, canonical_def.id,
                    NULL, now(), NULL, now()
                FROM pairs
                JOIN permission_definitions canonical_def
                    ON canonical_def.permission_code = pairs.canonical_code
                   AND canonical_def.is_active = TRUE
                JOIN permission_definitions mark_ready_def
                    ON mark_ready_def.permission_code = 'commerce.online_order.collection.mark_ready'
                   AND mark_ready_def.is_active = TRUE
                JOIN tenant_user_permissions tup
                    ON tup.permission_id = mark_ready_def.id
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DefinePermissionsSql);
            migrationBuilder.Sql(BackfillGrantsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_role_permissions
                WHERE notes = 'Granted alongside commerce.online_order.collection.mark_ready for pickup QR verification.';

                DELETE FROM tenant_user_permissions tup
                USING permission_definitions pd
                WHERE tup.permission_id = pd.id
                  AND pd.permission_code IN (
                      'commerce.online_order.collection.verify',
                      'commerce.online_order.collection.complete')
                  AND tup.id = md5(
                        'pickup-verify-user:' || tup.tenant_id::text || ':' ||
                        tup.user_id::text || ':' || pd.permission_code)::uuid;

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'commerce.online_order.collection.verify',
                    'commerce.online_order.collection.complete');
                """);
        }
    }
}
