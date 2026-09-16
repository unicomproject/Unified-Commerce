using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903090000_RepairTenantUserAccessAndOnlineOrderPermissions")]
public partial class RepairTenantUserAccessAndOnlineOrderPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH online_order_permissions(permission_code, action_type, description) AS (
                VALUES
                    ('commerce.online_order.orders.access', 'access', 'Access the POS online-order workspace.'),
                    ('commerce.online_order.orders.view', 'view', 'View online orders and order details.'),
                    ('commerce.online_order.fulfilment.start', 'start', 'Start fulfillment for an online order.'),
                    ('commerce.online_order.picking.view', 'view', 'View the online-order picking workflow.'),
                    ('commerce.online_order.picking.pick', 'pick', 'Mark an online-order item as picked.'),
                    ('commerce.online_order.picking.scan', 'scan', 'Scan an online-order item during picking.'),
                    ('commerce.online_order.picking.manual_entry', 'manual_entry', 'Enter an online-order item manually during picking.'),
                    ('commerce.online_order.picking.report_issue', 'report_issue', 'Report an online-order picking issue.'),
                    ('commerce.online_order.packing.view', 'view', 'View the online-order packing workflow.'),
                    ('commerce.online_order.packing.pack', 'pack', 'Pack a fulfilled online order.'),
                    ('commerce.online_order.collection.mark_ready', 'mark_ready', 'Mark an online order ready for collection.')
            )
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('permission:' || permission.permission_code)::uuid,
                permission.permission_code,
                feature.platform_module_id,
                feature.id,
                permission.action_type,
                permission.description,
                TRUE,
                TRUE,
                'TENANT',
                now(),
                now()
            FROM online_order_permissions permission
            JOIN platform_features feature
              ON feature.feature_code = 'click_collect'
             AND feature.status = 'ACTIVE'
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                scope = 'TENANT',
                updated_at = now();

            WITH role_permission_policy(role_code, permission_code) AS (
                VALUES
                    ('CASHIER', 'commerce.online_order.orders.access'),
                    ('CASHIER', 'commerce.online_order.orders.view'),
                    ('TENANT_ADMIN', 'commerce.online_order.orders.access'),
                    ('TENANT_ADMIN', 'commerce.online_order.orders.view'),
                    ('TENANT_ADMIN', 'commerce.online_order.fulfilment.start'),
                    ('TENANT_ADMIN', 'commerce.online_order.picking.view'),
                    ('TENANT_ADMIN', 'commerce.online_order.picking.pick'),
                    ('TENANT_ADMIN', 'commerce.online_order.picking.scan'),
                    ('TENANT_ADMIN', 'commerce.online_order.picking.manual_entry'),
                    ('TENANT_ADMIN', 'commerce.online_order.picking.report_issue'),
                    ('TENANT_ADMIN', 'commerce.online_order.packing.view'),
                    ('TENANT_ADMIN', 'commerce.online_order.packing.pack'),
                    ('TENANT_ADMIN', 'commerce.online_order.collection.mark_ready'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.orders.access'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.orders.view'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.fulfilment.start'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.picking.view'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.picking.pick'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.picking.scan'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.picking.manual_entry'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.picking.report_issue'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.packing.view'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.packing.pack'),
                    ('FULFILLMENT_STAFF', 'commerce.online_order.collection.mark_ready')
            )
            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
                granted_at, notes, created_at)
            SELECT
                md5(role.id::text || ':' || permission.id::text)::uuid,
                role.tenant_id,
                role.id,
                permission.id,
                NULL,
                now(),
                'Canonical POS online-order permission repair.',
                now()
            FROM role_permission_policy policy
            JOIN tenant_roles role
              ON role.role_code = policy.role_code
             AND role.is_active
            JOIN permission_definitions permission
              ON permission.permission_code = policy.permission_code
             AND permission.is_active
            JOIN tenant_feature_entitlements entitlement
              ON entitlement.tenant_id = role.tenant_id
            JOIN platform_features feature
              ON feature.id = entitlement.platform_feature_id
             AND feature.feature_code = 'click_collect'
             AND feature.status = 'ACTIVE'
            WHERE entitlement.entitlement_status = 'ENABLED'
              AND entitlement.is_enabled
              AND entitlement.revoked_at IS NULL
              AND entitlement.effective_from <= now()
              AND (entitlement.effective_until IS NULL OR entitlement.effective_until > now())
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;

            UPDATE tenant_users user_record
            SET user_type = CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM tenant_user_roles user_role
                        JOIN tenant_roles role ON role.id = user_role.role_id
                        WHERE user_role.tenant_id = user_record.tenant_id
                          AND user_role.user_id = user_record.id
                          AND user_role.revoked_at IS NULL
                          AND role.is_active
                          AND role.role_code = 'TENANT_ADMIN'
                    ) THEN 'admin'
                    ELSE 'standard'
                END,
                updated_at = now()
            WHERE user_record.user_type IS DISTINCT FROM CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM tenant_user_roles user_role
                        JOIN tenant_roles role ON role.id = user_role.role_id
                        WHERE user_role.tenant_id = user_record.tenant_id
                          AND user_role.user_id = user_record.id
                          AND user_role.revoked_at IS NULL
                          AND role.is_active
                          AND role.role_code = 'TENANT_ADMIN'
                    ) THEN 'admin'
                    ELSE 'standard'
                END;
            """);

        migrationBuilder.Sql("""
            DO $$
            DECLARE
                legacy_role RECORD;
            BEGIN
                FOR legacy_role IN
                    SELECT legacy.id AS legacy_role_id,
                           legacy.tenant_id,
                           system_cashier.id AS system_cashier_role_id
                    FROM tenant_roles legacy
                    JOIN tenant_roles system_cashier
                     ON system_cashier.tenant_id = legacy.tenant_id
                     AND system_cashier.role_code = 'CASHIER'
                     AND system_cashier.is_active
                     AND COALESCE(system_cashier.is_custom, FALSE) = FALSE
                    WHERE legacy.is_custom
                      AND legacy.is_active
                      AND legacy.role_code = 'CUSTOM_CASHIER'
                      AND lower(trim(legacy.role_name)) = 'custom cashier'
                      AND NOT EXISTS (
                          SELECT 1
                          FROM tenant_role_permissions permission
                          WHERE permission.tenant_id = legacy.tenant_id
                            AND permission.role_id = legacy.id
                            AND permission.revoked_at IS NULL)
                LOOP
                    INSERT INTO tenant_user_roles (
                        id, tenant_id, user_id, role_id,
                        assigned_by_tenant_user_id, assigned_at, revoked_at, created_at)
                    SELECT
                        md5(assignment.tenant_id::text || ':' || assignment.user_id::text || ':' ||
                            legacy_role.system_cashier_role_id::text)::uuid,
                        assignment.tenant_id,
                        assignment.user_id,
                        legacy_role.system_cashier_role_id,
                        assignment.assigned_by_tenant_user_id,
                        now(), NULL, now()
                    FROM tenant_user_roles assignment
                    WHERE assignment.tenant_id = legacy_role.tenant_id
                      AND assignment.role_id = legacy_role.legacy_role_id
                      AND assignment.revoked_at IS NULL
                    ON CONFLICT (tenant_id, user_id, role_id) DO UPDATE
                    SET revoked_at = NULL,
                        assigned_by_tenant_user_id = EXCLUDED.assigned_by_tenant_user_id,
                        assigned_at = EXCLUDED.assigned_at;

                    UPDATE tenant_user_roles
                    SET revoked_at = now()
                    WHERE tenant_id = legacy_role.tenant_id
                      AND role_id = legacy_role.legacy_role_id
                      AND revoked_at IS NULL;

                    INSERT INTO outlet_user_roles (
                        id, tenant_id, outlet_id, user_id, role_id,
                        assigned_by_tenant_user_id, assigned_at, revoked_by_tenant_user_id,
                        revoked_at, is_primary_manager, created_at)
                    SELECT
                        md5(assignment.tenant_id::text || ':' || assignment.outlet_id::text || ':' ||
                            assignment.user_id::text || ':' || legacy_role.system_cashier_role_id::text)::uuid,
                        assignment.tenant_id,
                        assignment.outlet_id,
                        assignment.user_id,
                        legacy_role.system_cashier_role_id,
                        assignment.assigned_by_tenant_user_id,
                        now(), NULL, NULL, FALSE, now()
                    FROM outlet_user_roles assignment
                    WHERE assignment.tenant_id = legacy_role.tenant_id
                      AND assignment.role_id = legacy_role.legacy_role_id
                      AND assignment.revoked_at IS NULL
                    ON CONFLICT (tenant_id, outlet_id, user_id, role_id) DO UPDATE
                    SET revoked_at = NULL,
                        revoked_by_tenant_user_id = NULL,
                        assigned_by_tenant_user_id = EXCLUDED.assigned_by_tenant_user_id,
                        assigned_at = EXCLUDED.assigned_at;

                    UPDATE outlet_user_roles
                    SET revoked_at = now(),
                        revoked_by_tenant_user_id = NULL
                    WHERE tenant_id = legacy_role.tenant_id
                      AND role_id = legacy_role.legacy_role_id
                      AND revoked_at IS NULL;

                    UPDATE tenant_roles
                    SET is_active = FALSE,
                        updated_at = now()
                    WHERE id = legacy_role.legacy_role_id;
                END LOOP;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data repair is intentionally irreversible. Role grants and user access may
        // have been edited by tenant administrators after this migration was applied.
    }
}
