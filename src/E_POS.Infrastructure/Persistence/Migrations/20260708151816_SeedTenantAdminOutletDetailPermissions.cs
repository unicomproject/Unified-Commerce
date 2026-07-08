using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTenantAdminOutletDetailPermissions : Migration
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
                    outlet_template.module_id,
                    outlet_template.feature_id,
                    seed.action_type,
                    seed.description,
                    true,
                    true,
                    now(),
                    now()
                FROM (
                    VALUES
                        ('77777777-0020-4000-8000-000000000001'::uuid, 'tenant.outlets.details.view', 'view', 'View outlet details'),
                        ('77777777-0021-4000-8000-000000000002'::uuid, 'tenant.outlets.revenue.view', 'view', 'View outlet revenue'),
                        ('77777777-0022-4000-8000-000000000003'::uuid, 'tenant.outlets.users.view', 'view', 'View outlet users'),
                        ('77777777-0023-4000-8000-000000000004'::uuid, 'tenant.outlets.tills.view', 'view', 'View outlet tills'),
                        ('77777777-0024-4000-8000-000000000005'::uuid, 'tenant.outlets.update', 'update', 'Update outlets'),
                        ('77777777-0025-4000-8000-000000000006'::uuid, 'tenant.users.view', 'view', 'View tenant users'),
                        ('77777777-0026-4000-8000-000000000007'::uuid, 'tenant.reports.sales.view', 'view', 'View sales reports')
                ) AS seed(id, permission_code, action_type, description)
                CROSS JOIN (
                    SELECT module_id, feature_id
                    FROM permission_definitions
                    WHERE permission_code = 'tenant.outlets.view'
                    LIMIT 1
                ) AS outlet_template
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
                    'Tenant admin outlet detail permission seed.',
                    now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'tenant.outlets.details.view'),
                        ('TENANT_ADMIN', 'tenant.outlets.revenue.view'),
                        ('TENANT_ADMIN', 'tenant.outlets.users.view'),
                        ('TENANT_ADMIN', 'tenant.outlets.tills.view'),
                        ('TENANT_ADMIN', 'tenant.outlets.update'),
                        ('TENANT_ADMIN', 'tenant.users.view'),
                        ('TENANT_ADMIN', 'tenant.reports.sales.view'),
                        ('STORE_MANAGER', 'tenant.outlets.details.view'),
                        ('STORE_MANAGER', 'tenant.outlets.revenue.view'),
                        ('STORE_MANAGER', 'tenant.outlets.users.view'),
                        ('STORE_MANAGER', 'tenant.outlets.tills.view')
                ) AS mapping(role_code, permission_code)
                JOIN tenant_roles
                    ON tenant_roles.role_code = mapping.role_code
                   AND tenant_roles.tenant_id = '55555555-0000-4000-8000-000000000001'
                JOIN permission_definitions
                    ON permission_definitions.permission_code = mapping.permission_code
                ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_role_permissions
                USING permission_definitions
                WHERE tenant_role_permissions.permission_id = permission_definitions.id
                  AND permission_definitions.permission_code IN (
                      'tenant.outlets.details.view',
                      'tenant.outlets.revenue.view',
                      'tenant.outlets.users.view',
                      'tenant.outlets.tills.view',
                      'tenant.outlets.update',
                      'tenant.users.view',
                      'tenant.reports.sales.view'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'tenant.outlets.details.view',
                    'tenant.outlets.revenue.view',
                    'tenant.outlets.users.view',
                    'tenant.outlets.tills.view',
                    'tenant.outlets.update',
                    'tenant.users.view',
                    'tenant.reports.sales.view'
                );
                """);
        }
    }
}
