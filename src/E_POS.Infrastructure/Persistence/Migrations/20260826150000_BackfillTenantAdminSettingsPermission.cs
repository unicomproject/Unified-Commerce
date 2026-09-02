using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260826150000_BackfillTenantAdminSettingsPermission")]
public partial class BackfillTenantAdminSettingsPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO role_template_version_permissions (
                id,
                role_template_version_id,
                permission_id,
                is_active,
                created_at)
            SELECT
                md5(role_template_versions.id::text || ':' || permission_definitions.id::text)::uuid,
                role_template_versions.id,
                permission_definitions.id,
                TRUE,
                now()
            FROM role_templates
            JOIN role_template_versions
              ON role_template_versions.role_template_id = role_templates.id
             AND role_template_versions.is_active
             AND role_template_versions.effective_from <= now()
             AND (role_template_versions.effective_until IS NULL
                  OR role_template_versions.effective_until > now())
            JOIN permission_definitions
              ON permission_definitions.permission_code = 'tenant.settings.manage'
             AND permission_definitions.is_active
            WHERE role_templates.template_code = 'TENANT_ADMIN'
              AND role_templates.is_active
            ON CONFLICT (role_template_version_id, permission_id) DO UPDATE
            SET is_active = TRUE;

            INSERT INTO tenant_role_permissions (
                id,
                tenant_id,
                role_id,
                permission_id,
                granted_by_tenant_user_id,
                granted_at,
                revoked_by_tenant_user_id,
                revoked_at,
                notes,
                created_at)
            SELECT
                md5(tenant_roles.id::text || ':' || permission_definitions.id::text)::uuid,
                tenant_roles.tenant_id,
                tenant_roles.id,
                permission_definitions.id,
                NULL,
                now(),
                NULL,
                NULL,
                'Tenant Admin settings access reconciliation.',
                now()
            FROM tenant_roles
            JOIN permission_definitions
              ON permission_definitions.permission_code = 'tenant.settings.manage'
             AND permission_definitions.is_active
            WHERE tenant_roles.role_code = 'TENANT_ADMIN'
              AND tenant_roles.is_active
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
        // Intentional no-op: access repair must not revoke a permission that may
        // have been granted independently after this migration was applied.
    }
}
