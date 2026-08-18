using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds Selected-Tenant Online Store bootstrap platform permission and grants it to super_administrator.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260812220000_SeedSelectedTenantOnlineStoreBootstrapPermission")]
public partial class SeedSelectedTenantOnlineStoreBootstrapPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO platform_permissions (id, permission_code, name, description, status, created_at, updated_at)
            SELECT * FROM (VALUES
                ('62000000-0000-0000-0000-000000000045'::uuid, 'platform.tenants.bootstrap.online_store.manage', 'Bootstrap Online Store', 'Configure initial Online Store bootstrap settings for a selected tenant.', 'ACTIVE', now(), now())
            ) AS seed(id, permission_code, name, description, status, created_at, updated_at)
            WHERE NOT EXISTS (
                SELECT 1 FROM platform_permissions existing WHERE existing.permission_code = seed.permission_code
            );

            UPDATE platform_permissions SET
                name = v.name,
                description = v.description,
                status = 'ACTIVE',
                updated_at = now()
            FROM (VALUES
                ('platform.tenants.bootstrap.online_store.manage', 'Bootstrap Online Store', 'Configure initial Online Store bootstrap settings for a selected tenant.')
            ) AS v(permission_code, name, description)
            WHERE platform_permissions.permission_code = v.permission_code;

            INSERT INTO platform_role_permissions (id, platform_role_id, platform_permission_id, description, created_at, updated_at)
            SELECT
                grants.id,
                role.id,
                perm.id,
                'TM-EPOS super administrator Selected-Tenant Online Store bootstrap seed.',
                now(),
                now()
            FROM platform_roles role
            CROSS JOIN platform_permissions perm
            CROSS JOIN (VALUES
                ('67000000-0000-0000-0000-000000000045'::uuid, 'platform.tenants.bootstrap.online_store.manage')
            ) AS grants(id, permission_code)
            WHERE role.role_code = 'super_administrator'
              AND perm.permission_code = grants.permission_code
              AND NOT EXISTS (
                  SELECT 1
                  FROM platform_role_permissions existing
                  WHERE existing.platform_role_id = role.id
                    AND existing.platform_permission_id = perm.id
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM platform_role_permissions
            WHERE platform_permission_id IN (
                SELECT id FROM platform_permissions
                WHERE permission_code IN (
                    'platform.tenants.bootstrap.online_store.manage'
                )
            );

            DELETE FROM platform_permissions
            WHERE permission_code IN (
                'platform.tenants.bootstrap.online_store.manage'
            );
            """);
    }
}
