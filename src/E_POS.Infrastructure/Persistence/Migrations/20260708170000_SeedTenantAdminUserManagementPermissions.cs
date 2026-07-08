using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260708170000_SeedTenantAdminUserManagementPermissions")]
    public partial class SeedTenantAdminUserManagementPermissions : Migration
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
                    users_template.module_id,
                    users_template.feature_id,
                    seed.action_type,
                    seed.description,
                    true,
                    true,
                    now(),
                    now()
                FROM (
                    VALUES
                        ('77777777-0040-4000-8000-000000000001'::uuid, 'tenant.users.create', 'create', 'Create tenant users'),
                        ('77777777-0041-4000-8000-000000000002'::uuid, 'tenant.users.invite', 'create', 'Invite tenant users'),
                        ('77777777-0042-4000-8000-000000000003'::uuid, 'tenant.users.update', 'update', 'Update tenant users'),
                        ('77777777-0043-4000-8000-000000000004'::uuid, 'tenant.users.delete', 'delete', 'Delete tenant users'),
                        ('77777777-0044-4000-8000-000000000005'::uuid, 'tenant.users.disable', 'update', 'Disable tenant users'),
                        ('77777777-0045-4000-8000-000000000006'::uuid, 'tenant.users.details.view', 'view', 'View tenant user details'),
                        ('77777777-0046-4000-8000-000000000007'::uuid, 'tenant.users.permission_override', 'update', 'Override tenant user permissions'),
                        ('77777777-0047-4000-8000-000000000008'::uuid, 'tenant.roles.view', 'view', 'View tenant roles'),
                        ('77777777-0048-4000-8000-000000000009'::uuid, 'tenant.permissions.view', 'view', 'View tenant permission catalog')
                ) AS seed(id, permission_code, action_type, description)
                CROSS JOIN (
                    SELECT module_id, feature_id
                    FROM permission_definitions
                    WHERE permission_code = 'tenant.users.view'
                    LIMIT 1
                ) AS users_template
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
                    'Tenant admin user management permission seed.',
                    now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'tenant.users.create'),
                        ('TENANT_ADMIN', 'tenant.users.invite'),
                        ('TENANT_ADMIN', 'tenant.users.update'),
                        ('TENANT_ADMIN', 'tenant.users.delete'),
                        ('TENANT_ADMIN', 'tenant.users.disable'),
                        ('TENANT_ADMIN', 'tenant.users.details.view'),
                        ('TENANT_ADMIN', 'tenant.users.permission_override'),
                        ('TENANT_ADMIN', 'tenant.roles.view'),
                        ('TENANT_ADMIN', 'tenant.permissions.view'),
                        ('STORE_MANAGER', 'tenant.users.details.view'),
                        ('STORE_MANAGER', 'tenant.roles.view')
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
                      'tenant.users.create',
                      'tenant.users.invite',
                      'tenant.users.update',
                      'tenant.users.delete',
                      'tenant.users.disable',
                      'tenant.users.details.view',
                      'tenant.users.permission_override',
                      'tenant.roles.view',
                      'tenant.permissions.view'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'tenant.users.create',
                    'tenant.users.invite',
                    'tenant.users.update',
                    'tenant.users.delete',
                    'tenant.users.disable',
                    'tenant.users.details.view',
                    'tenant.users.permission_override',
                    'tenant.roles.view',
                    'tenant.permissions.view'
                );
                """);
        }
    }
}
