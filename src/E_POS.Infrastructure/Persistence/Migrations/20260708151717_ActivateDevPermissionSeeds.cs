using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActivateDevPermissionSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE permission_definitions
                SET is_active = true,
                    updated_at = now()
                WHERE is_active = false;

                UPDATE tenant_roles
                SET is_active = true,
                    updated_at = now()
                WHERE is_active = false;

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
                SELECT *
                FROM (
                    VALUES
                        (
                            '77777777-0090-4000-8000-000000000001'::uuid,
                            'pos.home.view',
                            '00000000-0000-0000-0000-000000000001'::uuid,
                            '00000000-0000-0000-0000-000000000002'::uuid,
                            'view',
                            'View POS home dashboard',
                            true,
                            true,
                            now(),
                            now()
                        ),
                        (
                            '77777777-0091-4000-8000-000000000001'::uuid,
                            'pos.dashboard.view',
                            '00000000-0000-0000-0000-000000000001'::uuid,
                            '00000000-0000-0000-0000-000000000002'::uuid,
                            'view',
                            'View POS dashboard',
                            true,
                            true,
                            now(),
                            now()
                        )
                ) AS seed(id, permission_code, module_id, feature_id, action_type, description, is_system, is_active, created_at, updated_at)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM permission_definitions existing
                    WHERE existing.permission_code = seed.permission_code
                );

                UPDATE permission_definitions
                SET is_active = true,
                    updated_at = now()
                WHERE permission_code IN ('pos.home.view', 'pos.dashboard.view');

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
                    'Development POS dashboard permission seed.',
                    now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'pos.home.view'),
                        ('TENANT_ADMIN', 'pos.dashboard.view'),
                        ('STORE_MANAGER', 'pos.home.view'),
                        ('STORE_MANAGER', 'pos.dashboard.view'),
                        ('CASHIER', 'pos.home.view'),
                        ('CASHIER', 'pos.dashboard.view')
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
                  AND permission_definitions.permission_code IN ('pos.home.view', 'pos.dashboard.view');

                DELETE FROM permission_definitions
                WHERE permission_code IN ('pos.home.view', 'pos.dashboard.view');
                """);
        }
    }
}
