using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

public partial class SeedTenantAdminProductDashboardPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO permission_definitions (
                id,
                permission_code,
                module_id,
                feature_id,
                action_type,
                description,
                is_system,
                is_active,
                created_at,
                updated_at
            )
            SELECT
                seed.id,
                seed.permission_code,
                product_template.module_id,
                product_template.feature_id,
                seed.action_type,
                seed.description,
                true,
                true,
                now(),
                now()
            FROM (
                VALUES
                    ('77777777-0056-4000-8000-000000000001'::uuid, 'tenant.products.dashboard.view', 'view', 'View product dashboard'),
                    ('77777777-0057-4000-8000-000000000001'::uuid, 'tenant.stock.view', 'view', 'View tenant stock'),
                    ('77777777-0058-4000-8000-000000000001'::uuid, 'tenant.stock.in', 'create', 'Record stock in'),
                    ('77777777-0059-4000-8000-000000000001'::uuid, 'tenant.stock.out', 'create', 'Record stock out'),
                    ('77777777-005a-4000-8000-000000000001'::uuid, 'tenant.stock.value.view', 'view', 'View stock value'),
                    ('77777777-005b-4000-8000-000000000001'::uuid, 'tenant.stock.movements.view', 'view', 'View stock movements'),
                    ('77777777-005c-4000-8000-000000000001'::uuid, 'tenant.stock.expiry.view', 'view', 'View stock expiry alerts'),
                    ('77777777-005d-4000-8000-000000000001'::uuid, 'tenant.stock.adjustments.view', 'view', 'View stock adjustments'),
                    ('77777777-005e-4000-8000-000000000001'::uuid, 'tenant.stock.transfers.view', 'view', 'View stock transfers'),
                    ('77777777-005f-4000-8000-000000000001'::uuid, 'tenant.reports.products.view', 'view', 'View product reports')
            ) AS seed(id, permission_code, action_type, description)
            CROSS JOIN (
                SELECT module_id, feature_id
                FROM permission_definitions
                WHERE permission_code = 'catalog.products.view'
                LIMIT 1
            ) AS product_template
            WHERE NOT EXISTS (
                SELECT 1
                FROM permission_definitions existing
                WHERE existing.permission_code = seed.permission_code
            )
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO tenant_role_permissions (
                id,
                tenant_id,
                role_id,
                permission_id,
                notes,
                created_at
            )
            SELECT
                md5(mapping.role_code || ':' || mapping.permission_code)::uuid,
                '55555555-0000-4000-8000-000000000001',
                tenant_roles.id,
                permission_definitions.id,
                'Tenant admin product dashboard permission seed.',
                now()
            FROM (
                VALUES
                    ('TENANT_ADMIN', 'tenant.products.dashboard.view'),
                    ('TENANT_ADMIN', 'tenant.stock.view'),
                    ('TENANT_ADMIN', 'tenant.stock.in'),
                    ('TENANT_ADMIN', 'tenant.stock.out'),
                    ('TENANT_ADMIN', 'tenant.stock.movements.view'),
                    ('TENANT_ADMIN', 'tenant.stock.expiry.view'),
                    ('TENANT_ADMIN', 'tenant.reports.products.view'),
                    ('STORE_MANAGER', 'tenant.products.dashboard.view'),
                    ('STORE_MANAGER', 'tenant.stock.view'),
                    ('STORE_MANAGER', 'tenant.stock.in'),
                    ('STORE_MANAGER', 'tenant.stock.out'),
                    ('STORE_MANAGER', 'tenant.stock.movements.view'),
                    ('STORE_MANAGER', 'tenant.stock.expiry.view'),
                    ('STORE_MANAGER', 'tenant.reports.products.view'),
                    ('INVENTORY_MANAGER', 'tenant.products.dashboard.view'),
                    ('INVENTORY_MANAGER', 'tenant.stock.view'),
                    ('INVENTORY_MANAGER', 'tenant.stock.in'),
                    ('INVENTORY_MANAGER', 'tenant.stock.out'),
                    ('INVENTORY_MANAGER', 'tenant.stock.movements.view'),
                    ('INVENTORY_MANAGER', 'tenant.stock.expiry.view'),
                    ('INVENTORY_MANAGER', 'tenant.stock.adjustments.view'),
                    ('INVENTORY_MANAGER', 'tenant.stock.transfers.view'),
                    ('INVENTORY_MANAGER', 'tenant.reports.products.view')
            ) AS mapping(role_code, permission_code)
            JOIN tenant_roles
                ON tenant_roles.role_code = mapping.role_code
               AND tenant_roles.tenant_id = '55555555-0000-4000-8000-000000000001'
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
                'tenant.products.dashboard.view',
                'tenant.stock.view',
                'tenant.stock.in',
                'tenant.stock.out',
                'tenant.stock.value.view',
                'tenant.stock.movements.view',
                'tenant.stock.expiry.view',
                'tenant.stock.adjustments.view',
                'tenant.stock.transfers.view',
                'tenant.reports.products.view'
              );

            DELETE FROM permission_definitions
            WHERE permission_code IN (
                'tenant.products.dashboard.view',
                'tenant.stock.view',
                'tenant.stock.in',
                'tenant.stock.out',
                'tenant.stock.value.view',
                'tenant.stock.movements.view',
                'tenant.stock.expiry.view',
                'tenant.stock.adjustments.view',
                'tenant.stock.transfers.view',
                'tenant.reports.products.view'
            );
            """);
    }
}
