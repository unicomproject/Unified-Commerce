using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixInventoryDashboardPermissionSeed : Migration
    {
        /// <inheritdoc />
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
                        ('77777777-0060-4000-8000-000000000001'::uuid, 'tenant.stock.dashboard.view', 'view', 'View inventory dashboard')
                ) AS seed(id, permission_code, action_type, description)
                CROSS JOIN (
                    SELECT module_id, feature_id
                    FROM permission_definitions
                    WHERE permission_code = 'inventory.stock.view'
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
                    md5(tenant_roles.tenant_id::text || ':' || mapping.role_code || ':' || mapping.permission_code)::uuid,
                    tenant_roles.tenant_id,
                    tenant_roles.id,
                    permission_definitions.id,
                    'Tenant admin inventory dashboard permission seed for all tenants.',
                    now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'tenant.stock.dashboard.view'),
                        ('STORE_MANAGER', 'tenant.stock.dashboard.view'),
                        ('INVENTORY_MANAGER', 'tenant.stock.dashboard.view')
                ) AS mapping(role_code, permission_code)
                JOIN tenant_roles ON tenant_roles.role_code = mapping.role_code
                JOIN permission_definitions ON permission_definitions.permission_code = mapping.permission_code
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM tenant_role_permissions existing
                    WHERE existing.role_id = tenant_roles.id
                    AND existing.permission_id = permission_definitions.id
                )
                ON CONFLICT (id) DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
