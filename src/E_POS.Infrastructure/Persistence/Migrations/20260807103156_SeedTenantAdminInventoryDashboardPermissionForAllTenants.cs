using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTenantAdminInventoryDashboardPermissionForAllTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
