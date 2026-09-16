using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903100000_CompleteTenantAdminGranularPermissions")]
public partial class CompleteTenantAdminGranularPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH missing_permissions(permission_code, action_type, description, template_code) AS (
                VALUES
                    ('tenant.outlets.create', 'create', 'Create tenant outlets.', 'tenant.outlets.manage'),
                    ('tenant.outlets.delete', 'delete', 'Delete tenant outlets.', 'tenant.outlets.manage'),
                    ('tenant.roles.create', 'create', 'Create tenant roles.', 'tenant.roles.manage'),
                    ('tenant.roles.update', 'update', 'Update tenant roles.', 'tenant.roles.manage'),
                    ('tenant.roles.delete', 'delete', 'Delete tenant roles.', 'tenant.roles.manage'),
                    ('tenant.roles.permissions.view', 'view', 'View permissions assigned to tenant roles.', 'tenant.roles.manage'),
                    ('tenant.roles.permissions.update', 'update', 'Update permissions assigned to tenant roles.', 'tenant.roles.manage'),
                    ('tenant.roles.assignments.view', 'view', 'View users assigned to tenant roles.', 'tenant.roles.manage'),
                    ('tenant.roles.assignments.update', 'update', 'Update users assigned to tenant roles.', 'tenant.roles.manage')
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
                TRUE,
                TRUE,
                'TENANT',
                now(),
                now()
            FROM missing_permissions requested
            JOIN permission_definitions template
              ON template.permission_code = requested.template_code
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                scope = 'TENANT',
                updated_at = now();

            WITH tenant_admin_permissions(permission_code) AS (
                VALUES
                    ('tenant.outlets.create'),
                    ('tenant.outlets.delete'),
                    ('tenant.outlets.details.view'),
                    ('tenant.roles.create'),
                    ('tenant.roles.update'),
                    ('tenant.roles.delete'),
                    ('tenant.roles.permissions.view'),
                    ('tenant.roles.permissions.update'),
                    ('tenant.roles.assignments.view'),
                    ('tenant.roles.assignments.update'),
                    ('tenant.permissions.view')
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
                'Canonical granular Tenant Admin permission completion.',
                now()
            FROM tenant_roles role
            CROSS JOIN tenant_admin_permissions requested
            JOIN permission_definitions permission
              ON permission.permission_code = requested.permission_code
             AND permission.is_active
            WHERE role.role_code = 'TENANT_ADMIN'
              AND role.is_active
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Permission grants are intentionally retained because administrators may
        // have delegated or edited them after this data repair was applied.
    }
}
