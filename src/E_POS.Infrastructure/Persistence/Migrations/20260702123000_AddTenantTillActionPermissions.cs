using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260702123000_AddTenantTillActionPermissions")]
    public partial class AddTenantTillActionPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0017-4000-8000-000000000001', 'tenant.tills.view', 'View Tills', 'ACTIVE', now(), now()),
                    ('77777777-0018-4000-8000-000000000001', 'tenant.tills.create', 'Create Tills', 'ACTIVE', now(), now()),
                    ('77777777-0019-4000-8000-000000000001', 'tenant.tills.update', 'Update Tills', 'ACTIVE', now(), now()),
                    ('77777777-0020-4000-8000-000000000001', 'tenant.tills.delete', 'Delete Tills', 'ACTIVE', now(), now()),
                    ('77777777-0021-4000-8000-000000000001', 'tenant.tills.manage', 'Manage Tills', 'ACTIVE', now(), now())
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
                       'Development role permission seed.',
                       now(),
                       now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'tenant.tills.view'),
                        ('TENANT_ADMIN', 'tenant.tills.create'),
                        ('TENANT_ADMIN', 'tenant.tills.update'),
                        ('TENANT_ADMIN', 'tenant.tills.delete'),
                        ('TENANT_ADMIN', 'tenant.tills.manage'),
                        ('STORE_MANAGER', 'tenant.tills.view'),
                        ('STORE_MANAGER', 'tenant.tills.create'),
                        ('STORE_MANAGER', 'tenant.tills.update')
                ) AS mapping(role_code, permission_code)
                JOIN tenant_roles ON tenant_roles.role_code = mapping.role_code
                    AND tenant_roles.tenant_id = '55555555-0000-4000-8000-000000000001'
                JOIN permission_definitions ON permission_definitions.permission_code = mapping.permission_code
                ON CONFLICT (tenant_role_id, permission_definition_id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_role_permissions
                USING permission_definitions
                WHERE tenant_role_permissions.permission_definition_id = permission_definitions.id
                  AND permission_definitions.permission_code IN (
                      'tenant.tills.view',
                      'tenant.tills.create',
                      'tenant.tills.update',
                      'tenant.tills.delete',
                      'tenant.tills.manage'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'tenant.tills.view',
                    'tenant.tills.create',
                    'tenant.tills.update',
                    'tenant.tills.delete',
                    'tenant.tills.manage'
                );
                """);
        }
    }
}