using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds `platform.tenant_subscriptions.view` to the platform permission catalogue and grants it to `super_administrator`.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260729153000_SeedTenantSubscriptionsViewPermission")]
public partial class SeedTenantSubscriptionsViewPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO platform_permissions (id, permission_code, name, description, status, created_at, updated_at)
            SELECT
                '62000000-0000-0000-0000-000000000037'::uuid,
                'platform.tenant_subscriptions.view',
                'View Tenant Subscriptions',
                'View tenant-level subscription lifecycle and dashboard subscription widgets.',
                'ACTIVE',
                now(),
                now()
            WHERE NOT EXISTS (
                SELECT 1
                FROM platform_permissions
                WHERE permission_code = 'platform.tenant_subscriptions.view'
            );

            UPDATE platform_permissions
            SET name = 'View Tenant Subscriptions',
                description = 'View tenant-level subscription lifecycle and dashboard subscription widgets.',
                status = 'ACTIVE',
                updated_at = now()
            WHERE permission_code = 'platform.tenant_subscriptions.view';

            INSERT INTO platform_role_permissions (id, platform_role_id, platform_permission_id, description, created_at, updated_at)
            SELECT
                '67000000-0000-0000-0000-000000000037'::uuid,
                role.id,
                perm.id,
                'TM-EPOS super administrator permission seed.',
                now(),
                now()
            FROM platform_roles role
            CROSS JOIN platform_permissions perm
            WHERE role.role_code = 'super_administrator'
              AND perm.permission_code = 'platform.tenant_subscriptions.view'
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
        migrationBuilder.Sql($"""
            DELETE FROM platform_role_permissions
            WHERE platform_permission_id = (
                SELECT id FROM platform_permissions
                WHERE permission_code = 'platform.tenant_subscriptions.view'
            );

            DELETE FROM platform_permissions
            WHERE permission_code = 'platform.tenant_subscriptions.view';
            """);
    }
}
