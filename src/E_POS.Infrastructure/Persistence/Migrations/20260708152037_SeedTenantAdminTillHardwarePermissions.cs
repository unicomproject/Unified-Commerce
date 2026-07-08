using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTenantAdminTillHardwarePermissions : Migration
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
                    till_template.module_id,
                    till_template.feature_id,
                    seed.action_type,
                    seed.description,
                    true,
                    true,
                    now(),
                    now()
                FROM (
                    VALUES
                        ('77777777-0030-4000-8000-000000000001'::uuid, 'tenant.tills.details.view', 'view', 'View till details'),
                        ('77777777-0031-4000-8000-000000000002'::uuid, 'tenant.tills.assign_outlet', 'update', 'Assign till outlet'),
                        ('77777777-0032-4000-8000-000000000003'::uuid, 'tenant.hardware.view', 'view', 'View till hardware'),
                        ('77777777-0033-4000-8000-000000000004'::uuid, 'tenant.hardware.manage', 'update', 'Manage till hardware')
                ) AS seed(id, permission_code, action_type, description)
                CROSS JOIN (
                    SELECT module_id, feature_id
                    FROM permission_definitions
                    WHERE permission_code = 'tenant.tills.view'
                    LIMIT 1
                ) AS till_template
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
                    'Tenant admin till management permission seed.',
                    now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'tenant.tills.details.view'),
                        ('TENANT_ADMIN', 'tenant.tills.assign_outlet'),
                        ('TENANT_ADMIN', 'tenant.hardware.view'),
                        ('TENANT_ADMIN', 'tenant.hardware.manage'),
                        ('STORE_MANAGER', 'tenant.tills.details.view'),
                        ('STORE_MANAGER', 'tenant.hardware.view')
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
                      'tenant.tills.details.view',
                      'tenant.tills.assign_outlet',
                      'tenant.hardware.view',
                      'tenant.hardware.manage'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'tenant.tills.details.view',
                    'tenant.tills.assign_outlet',
                    'tenant.hardware.view',
                    'tenant.hardware.manage'
                );
                """);
        }
    }
}
