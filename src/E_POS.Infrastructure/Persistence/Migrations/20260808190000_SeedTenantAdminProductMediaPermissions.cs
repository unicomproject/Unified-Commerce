using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260808190000_SeedTenantAdminProductMediaPermissions")]
public partial class SeedTenantAdminProductMediaPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO permission_definitions (
                id,
                permission_code,
                action_type,
                description,
                is_system,
                is_active,
                created_at,
                updated_at
            )
            VALUES
                ('77777777-0063-4000-8000-000000000001'::uuid, 'catalog.product_media.manage', 'update', 'Manage product images and media uploads', true, true, now(), now()),
                ('77777777-0064-4000-8000-000000000001'::uuid, 'catalog.product_channels.manage', 'update', 'Manage product channel visibility', true, true, now(), now()),
                ('77777777-0065-4000-8000-000000000001'::uuid, 'tenant.product_media.manage', 'update', 'Manage tenant product media', true, true, now(), now())
            ON CONFLICT (permission_code) DO UPDATE
            SET description = EXCLUDED.description,
                is_active = true,
                updated_at = now();

            INSERT INTO tenant_role_permissions (
                id,
                tenant_id,
                role_id,
                permission_id,
                notes,
                created_at
            )
            SELECT
                md5(tenant_roles.id::text || ':' || permission_definitions.permission_code)::uuid,
                tenant_roles.tenant_id,
                tenant_roles.id,
                permission_definitions.id,
                'Tenant admin product media and channel permissions seed.',
                now()
            FROM (
                VALUES
                    ('TENANT_ADMIN', 'catalog.product_media.manage'),
                    ('TENANT_ADMIN', 'catalog.product_channels.manage'),
                    ('TENANT_ADMIN', 'tenant.product_media.manage'),
                    ('STORE_MANAGER', 'catalog.product_media.manage'),
                    ('STORE_MANAGER', 'catalog.product_channels.manage'),
                    ('STORE_MANAGER', 'tenant.product_media.manage'),
                    ('INVENTORY_MANAGER', 'catalog.product_media.manage'),
                    ('INVENTORY_MANAGER', 'catalog.product_channels.manage'),
                    ('INVENTORY_MANAGER', 'tenant.product_media.manage')
            ) AS mapping(role_code, permission_code)
            JOIN tenant_roles
                ON tenant_roles.role_code = mapping.role_code
            JOIN permission_definitions
                ON permission_definitions.permission_code = mapping.permission_code
            ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM tenant_role_permissions
            USING permission_definitions
            WHERE tenant_role_permissions.permission_id = permission_definitions.id
              AND permission_definitions.permission_code IN (
                'catalog.product_media.manage',
                'catalog.product_channels.manage',
                'tenant.product_media.manage'
              );

            DELETE FROM permission_definitions
            WHERE permission_code IN (
                'catalog.product_media.manage',
                'catalog.product_channels.manage',
                'tenant.product_media.manage'
            );
            """);
    }
}
