using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandCollectionCrudPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0121-4000-8000-000000000001', 'catalog.brands.view', 'View Brands', 'ACTIVE', now(), now()),
                    ('77777777-0122-4000-8000-000000000001', 'catalog.brands.create', 'Create Brands', 'ACTIVE', now(), now()),
                    ('77777777-0123-4000-8000-000000000001', 'catalog.brands.update', 'Update Brands', 'ACTIVE', now(), now()),
                    ('77777777-0124-4000-8000-000000000001', 'catalog.brands.delete', 'Delete Brands', 'ACTIVE', now(), now()),
                    ('77777777-0125-4000-8000-000000000001', 'catalog.brands.manage', 'Manage Brands', 'ACTIVE', now(), now()),
                    ('77777777-0131-4000-8000-000000000001', 'catalog.collections.view', 'View Collections', 'ACTIVE', now(), now()),
                    ('77777777-0132-4000-8000-000000000001', 'catalog.collections.create', 'Create Collections', 'ACTIVE', now(), now()),
                    ('77777777-0133-4000-8000-000000000001', 'catalog.collections.update', 'Update Collections', 'ACTIVE', now(), now()),
                    ('77777777-0134-4000-8000-000000000001', 'catalog.collections.delete', 'Delete Collections', 'ACTIVE', now(), now()),
                    ('77777777-0135-4000-8000-000000000001', 'catalog.collections.manage', 'Manage Collections', 'ACTIVE', now(), now())
                ON CONFLICT (permission_code) DO UPDATE
                SET name = EXCLUDED.name,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO tenant_role_permissions (id, tenant_role_id, permission_definition_id, description, created_at, updated_at)
                SELECT md5(mapping.role_code || ':' || mapping.permission_code)::uuid,
                       tenant_roles.id,
                       permission_definitions.id,
                       'Development catalog brand collection permission seed.',
                       now(),
                       now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'catalog.brands.view'),
                        ('TENANT_ADMIN', 'catalog.brands.create'),
                        ('TENANT_ADMIN', 'catalog.brands.update'),
                        ('TENANT_ADMIN', 'catalog.brands.delete'),
                        ('TENANT_ADMIN', 'catalog.brands.manage'),
                        ('TENANT_ADMIN', 'catalog.collections.view'),
                        ('TENANT_ADMIN', 'catalog.collections.create'),
                        ('TENANT_ADMIN', 'catalog.collections.update'),
                        ('TENANT_ADMIN', 'catalog.collections.delete'),
                        ('TENANT_ADMIN', 'catalog.collections.manage'),
                        ('STORE_MANAGER', 'catalog.brands.view'),
                        ('STORE_MANAGER', 'catalog.brands.manage'),
                        ('STORE_MANAGER', 'catalog.collections.view'),
                        ('STORE_MANAGER', 'catalog.collections.manage'),
                        ('INVENTORY_MANAGER', 'catalog.brands.view'),
                        ('INVENTORY_MANAGER', 'catalog.collections.view')
                ) AS mapping(role_code, permission_code)
                JOIN tenant_roles ON tenant_roles.role_code = mapping.role_code
                    AND tenant_roles.tenant_id = '55555555-0000-4000-8000-000000000001'
                JOIN permission_definitions ON permission_definitions.permission_code = mapping.permission_code
                ON CONFLICT (tenant_role_id, permission_definition_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_role_permissions
                USING permission_definitions
                WHERE tenant_role_permissions.permission_definition_id = permission_definitions.id
                  AND permission_definitions.permission_code IN (
                      'catalog.brands.view',
                      'catalog.brands.create',
                      'catalog.brands.update',
                      'catalog.brands.delete',
                      'catalog.brands.manage',
                      'catalog.collections.view',
                      'catalog.collections.create',
                      'catalog.collections.update',
                      'catalog.collections.delete',
                      'catalog.collections.manage'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'catalog.brands.view',
                    'catalog.brands.create',
                    'catalog.brands.update',
                    'catalog.brands.delete',
                    'catalog.brands.manage',
                    'catalog.collections.view',
                    'catalog.collections.create',
                    'catalog.collections.update',
                    'catalog.collections.delete',
                    'catalog.collections.manage'
                );
                """);
        }
    }
}