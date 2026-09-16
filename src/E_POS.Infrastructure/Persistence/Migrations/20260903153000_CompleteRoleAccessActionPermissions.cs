using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903153000_CompleteRoleAccessActionPermissions")]
public partial class CompleteRoleAccessActionPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH requested(permission_code, action_type, description) AS (
                VALUES
                    ('tenant.roles.status.update', 'update', 'Activate or deactivate tenant roles.'),
                    ('tenant.roles.users.assign', 'update', 'Assign or remove users from tenant roles.'),
                    ('tenant.roles.outlets.assign', 'update', 'Configure outlet scope for tenant role assignments.')
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
              ON template.permission_code = 'tenant.roles.manage'
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                scope = 'TENANT',
                updated_at = now();

            WITH grants(permission_code, predecessor_code) AS (
                VALUES
                    ('tenant.roles.status.update', 'tenant.roles.update'),
                    ('tenant.roles.users.assign', 'tenant.roles.assignments.update'),
                    ('tenant.roles.outlets.assign', 'tenant.roles.assignments.update')
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
                'Granular Role & Access action permission completion.',
                now()
            FROM tenant_roles role
            CROSS JOIN grants
            JOIN permission_definitions permission
              ON permission.permission_code = grants.permission_code
             AND permission.is_active
            WHERE role.is_active
              AND (
                    role.role_code = 'TENANT_ADMIN'
                    OR EXISTS (
                        SELECT 1
                        FROM tenant_role_permissions existing_grant
                        JOIN permission_definitions existing_permission
                          ON existing_permission.id = existing_grant.permission_id
                        WHERE existing_grant.tenant_id = role.tenant_id
                          AND existing_grant.role_id = role.id
                          AND existing_grant.revoked_at IS NULL
                          AND existing_permission.permission_code IN (
                              'tenant.roles.manage', grants.predecessor_code))
              )
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
