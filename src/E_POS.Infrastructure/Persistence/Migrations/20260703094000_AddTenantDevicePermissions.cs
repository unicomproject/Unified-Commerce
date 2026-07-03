using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260703094000_AddTenantDevicePermissions")]
    public partial class AddTenantDevicePermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0022-4000-8000-000000000001', 'tenant.devices.view', 'View POS Devices', 'ACTIVE', now(), now()),
                    ('77777777-0023-4000-8000-000000000001', 'tenant.devices.create', 'Create POS Devices', 'ACTIVE', now(), now()),
                    ('77777777-0024-4000-8000-000000000001', 'tenant.devices.update', 'Update POS Devices', 'ACTIVE', now(), now()),
                    ('77777777-0025-4000-8000-000000000001', 'tenant.devices.delete', 'Delete POS Devices', 'ACTIVE', now(), now()),
                    ('77777777-0026-4000-8000-000000000001', 'tenant.devices.manage', 'Manage POS Devices', 'ACTIVE', now(), now())
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
                        ('TENANT_ADMIN', 'tenant.devices.view'),
                        ('TENANT_ADMIN', 'tenant.devices.create'),
                        ('TENANT_ADMIN', 'tenant.devices.update'),
                        ('TENANT_ADMIN', 'tenant.devices.delete'),
                        ('TENANT_ADMIN', 'tenant.devices.manage'),
                        ('STORE_MANAGER', 'tenant.devices.view'),
                        ('STORE_MANAGER', 'tenant.devices.create'),
                        ('STORE_MANAGER', 'tenant.devices.update')
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
                      'tenant.devices.view',
                      'tenant.devices.create',
                      'tenant.devices.update',
                      'tenant.devices.delete',
                      'tenant.devices.manage'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'tenant.devices.view',
                    'tenant.devices.create',
                    'tenant.devices.update',
                    'tenant.devices.delete',
                    'tenant.devices.manage'
                );
                """);
        }
    }
}