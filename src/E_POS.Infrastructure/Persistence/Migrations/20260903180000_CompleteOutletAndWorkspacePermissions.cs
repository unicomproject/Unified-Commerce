using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903180000_CompleteOutletAndWorkspacePermissions")]
public partial class CompleteOutletAndWorkspacePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH requested(permission_code, action_type, description, template_code) AS (
                VALUES
                    ('tenant.outlets.status.update', 'update', 'Activate or deactivate tenant outlets.', 'tenant.outlets.manage'),
                    ('tenant.outlets.manager.assign', 'update', 'Assign or remove tenant outlet managers.', 'tenant.outlets.manage'),
                    ('tenant.outlets.image.update', 'update', 'Add, replace, or remove tenant outlet images.', 'tenant.outlets.manage'),
                    ('workspace.tenant_admin.access', 'access', 'Access the Tenant Admin workspace.', 'tenant.users.manage'),
                    ('workspace.pos.access', 'access', 'Access the POS / Cashier workspace.', 'pos.home.view')
            )
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('permission:' || requested.permission_code)::uuid,
                requested.permission_code,
                template.module_id,
                template.feature_id,
                requested.action_type,
                requested.description,
                TRUE, TRUE, 'TENANT', now(), now()
            FROM requested
            JOIN permission_definitions template
              ON template.permission_code = requested.template_code
            ON CONFLICT (permission_code) DO UPDATE
            SET action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                scope = 'TENANT',
                updated_at = now();

            WITH outlet_permissions(permission_code) AS (
                VALUES
                    ('tenant.outlets.status.update'),
                    ('tenant.outlets.manager.assign'),
                    ('tenant.outlets.image.update')
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
                'Backfill granular Outlet action permissions.',
                now()
            FROM tenant_roles role
            CROSS JOIN outlet_permissions requested
            JOIN permission_definitions permission
              ON permission.permission_code = requested.permission_code
             AND permission.is_active
            WHERE role.is_active
              AND EXISTS (
                    SELECT 1
                    FROM tenant_role_permissions grant_row
                    JOIN permission_definitions granted_permission
                      ON granted_permission.id = grant_row.permission_id
                    WHERE grant_row.tenant_id = role.tenant_id
                      AND grant_row.role_id = role.id
                      AND grant_row.revoked_at IS NULL
                      AND granted_permission.permission_code IN (
                          'tenant.outlets.update', 'tenant.outlets.manage'))
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;

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
                'Backfill strict Tenant Admin workspace access.',
                now()
            FROM tenant_roles role
            JOIN permission_definitions permission
              ON permission.permission_code = 'workspace.tenant_admin.access'
             AND permission.is_active
            WHERE role.is_active
              AND (
                    role.role_code = 'TENANT_ADMIN'
                    OR (
                        role.role_code <> 'CASHIER'
                        AND EXISTS (
                            SELECT 1
                            FROM tenant_role_permissions grant_row
                            JOIN permission_definitions granted_permission
                              ON granted_permission.id = grant_row.permission_id
                            WHERE grant_row.tenant_id = role.tenant_id
                              AND grant_row.role_id = role.id
                              AND grant_row.revoked_at IS NULL
                              AND (
                                  granted_permission.permission_code LIKE 'tenant.%'
                                  OR granted_permission.permission_code LIKE 'tenant_admin.%'
                                  OR granted_permission.permission_code LIKE 'dashboard.%'
                                  OR granted_permission.permission_code LIKE 'outlet.%'
                                  OR granted_permission.permission_code LIKE 'user.%'
                                  OR granted_permission.permission_code LIKE 'role.%'
                                  OR granted_permission.permission_code LIKE 'permission.%'
                                  OR granted_permission.permission_code LIKE 'inventory.%'
                                  OR granted_permission.permission_code LIKE 'report.%'))))
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;

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
                'Backfill strict POS workspace access.',
                now()
            FROM tenant_roles role
            JOIN permission_definitions permission
              ON permission.permission_code = 'workspace.pos.access'
             AND permission.is_active
            WHERE role.is_active
              AND (
                    role.role_code = 'CASHIER'
                    OR EXISTS (
                        SELECT 1
                        FROM tenant_role_permissions grant_row
                        JOIN permission_definitions granted_permission
                          ON granted_permission.id = grant_row.permission_id
                        WHERE grant_row.tenant_id = role.tenant_id
                          AND grant_row.role_id = role.id
                          AND grant_row.revoked_at IS NULL
                          AND (
                              granted_permission.permission_code LIKE 'pos.%'
                              OR granted_permission.permission_code LIKE 'sales.%'
                              OR granted_permission.permission_code LIKE 'payments.%'
                              OR granted_permission.permission_code LIKE 'receipts.%'
                              OR granted_permission.permission_code LIKE 'returns.%'
                              OR granted_permission.permission_code LIKE 'refunds.%'
                              OR granted_permission.permission_code LIKE 'exchanges.%'
                              OR granted_permission.permission_code LIKE 'cash_drawer.%')))
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Retained intentionally because administrators may delegate these grants.
    }
}
