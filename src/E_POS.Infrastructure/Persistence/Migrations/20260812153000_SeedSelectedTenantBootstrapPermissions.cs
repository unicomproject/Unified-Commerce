using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds Selected-Tenant bootstrap platform permissions and grants them to super_administrator.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260812153000_SeedSelectedTenantBootstrapPermissions")]
public partial class SeedSelectedTenantBootstrapPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO platform_permissions (id, permission_code, name, description, status, created_at, updated_at)
            SELECT * FROM (VALUES
                ('62000000-0000-0000-0000-000000000038'::uuid, 'platform.tenants.bootstrap.access', 'Selected Tenant Bootstrap Access', 'Enter Selected-Tenant mode and view the setup hub.', 'ACTIVE', now(), now()),
                ('62000000-0000-0000-0000-000000000039'::uuid, 'platform.tenants.bootstrap.outlets.manage', 'Bootstrap Outlets', 'Create bootstrap outlets for a selected tenant.', 'ACTIVE', now(), now()),
                ('62000000-0000-0000-0000-000000000040'::uuid, 'platform.tenants.bootstrap.tills.manage', 'Bootstrap Tills', 'Create bootstrap tills for a selected tenant.', 'ACTIVE', now(), now()),
                ('62000000-0000-0000-0000-000000000041'::uuid, 'platform.tenants.bootstrap.roles.manage', 'Bootstrap Roles', 'Create bootstrap tenant roles for a selected tenant.', 'ACTIVE', now(), now()),
                ('62000000-0000-0000-0000-000000000042'::uuid, 'platform.tenants.bootstrap.users.manage', 'Bootstrap Users', 'Add bootstrap tenant users for a selected tenant.', 'ACTIVE', now(), now()),
                ('62000000-0000-0000-0000-000000000043'::uuid, 'platform.tenants.bootstrap.products.manage', 'Bootstrap Products', 'Create bootstrap products for a selected tenant.', 'ACTIVE', now(), now()),
                ('62000000-0000-0000-0000-000000000044'::uuid, 'platform.tenants.bootstrap.products.import', 'Bootstrap Product Import', 'Import bootstrap products via CSV for a selected tenant.', 'ACTIVE', now(), now())
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
                ('platform.tenants.bootstrap.access', 'Selected Tenant Bootstrap Access', 'Enter Selected-Tenant mode and view the setup hub.'),
                ('platform.tenants.bootstrap.outlets.manage', 'Bootstrap Outlets', 'Create bootstrap outlets for a selected tenant.'),
                ('platform.tenants.bootstrap.tills.manage', 'Bootstrap Tills', 'Create bootstrap tills for a selected tenant.'),
                ('platform.tenants.bootstrap.roles.manage', 'Bootstrap Roles', 'Create bootstrap tenant roles for a selected tenant.'),
                ('platform.tenants.bootstrap.users.manage', 'Bootstrap Users', 'Add bootstrap tenant users for a selected tenant.'),
                ('platform.tenants.bootstrap.products.manage', 'Bootstrap Products', 'Create bootstrap products for a selected tenant.'),
                ('platform.tenants.bootstrap.products.import', 'Bootstrap Product Import', 'Import bootstrap products via CSV for a selected tenant.')
            ) AS v(permission_code, name, description)
            WHERE platform_permissions.permission_code = v.permission_code;

            INSERT INTO platform_role_permissions (id, platform_role_id, platform_permission_id, description, created_at, updated_at)
            SELECT
                grants.id,
                role.id,
                perm.id,
                'TM-EPOS super administrator Selected-Tenant bootstrap seed.',
                now(),
                now()
            FROM platform_roles role
            CROSS JOIN platform_permissions perm
            CROSS JOIN (VALUES
                ('67000000-0000-0000-0000-000000000038'::uuid, 'platform.tenants.bootstrap.access'),
                ('67000000-0000-0000-0000-000000000039'::uuid, 'platform.tenants.bootstrap.outlets.manage'),
                ('67000000-0000-0000-0000-000000000040'::uuid, 'platform.tenants.bootstrap.tills.manage'),
                ('67000000-0000-0000-0000-000000000041'::uuid, 'platform.tenants.bootstrap.roles.manage'),
                ('67000000-0000-0000-0000-000000000042'::uuid, 'platform.tenants.bootstrap.users.manage'),
                ('67000000-0000-0000-0000-000000000043'::uuid, 'platform.tenants.bootstrap.products.manage'),
                ('67000000-0000-0000-0000-000000000044'::uuid, 'platform.tenants.bootstrap.products.import')
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
                    'platform.tenants.bootstrap.access',
                    'platform.tenants.bootstrap.outlets.manage',
                    'platform.tenants.bootstrap.tills.manage',
                    'platform.tenants.bootstrap.roles.manage',
                    'platform.tenants.bootstrap.users.manage',
                    'platform.tenants.bootstrap.products.manage',
                    'platform.tenants.bootstrap.products.import'
                )
            );

            DELETE FROM platform_permissions
            WHERE permission_code IN (
                'platform.tenants.bootstrap.access',
                'platform.tenants.bootstrap.outlets.manage',
                'platform.tenants.bootstrap.tills.manage',
                'platform.tenants.bootstrap.roles.manage',
                'platform.tenants.bootstrap.users.manage',
                'platform.tenants.bootstrap.products.manage',
                'platform.tenants.bootstrap.products.import'
            );
            """);
    }
}
