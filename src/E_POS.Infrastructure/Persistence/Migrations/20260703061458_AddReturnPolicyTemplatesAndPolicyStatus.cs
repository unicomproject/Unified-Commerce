using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnPolicyTemplatesAndPolicyStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "return_policies",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.CreateTable(
                name: "return_policy_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    return_window_days = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_policy_templates", x => x.id);
                    table.CheckConstraint("ck_return_policy_templates_return_window_days", "return_window_days IS NULL OR return_window_days >= 0");
                    table.CheckConstraint("ck_return_policy_templates_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_policies_status",
                table: "return_policies",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_return_policy_templates_template_code",
                table: "return_policy_templates",
                column: "template_code",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO return_policy_templates (id, template_code, name, return_window_days, status, created_at, updated_at)
                VALUES
                    ('88888888-0001-4000-8000-000000000001', 'NO_RETURN', 'No Return', 0, 'ACTIVE', now(), now()),
                    ('88888888-0002-4000-8000-000000000001', 'SAME_DAY', 'Same Day Return', 0, 'ACTIVE', now(), now()),
                    ('88888888-0003-4000-8000-000000000001', '7DAYS', '7 Days Return', 7, 'ACTIVE', now(), now()),
                    ('88888888-0004-4000-8000-000000000001', '14DAYS', '14 Days Return', 14, 'ACTIVE', now(), now())
                ON CONFLICT (template_code) DO UPDATE
                SET name = EXCLUDED.name,
                    return_window_days = EXCLUDED.return_window_days,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO platform_permissions (id, permission_code, name, description, status, created_at, updated_at)
                VALUES
                    ('62000000-0000-0000-0000-000000000032', 'platform.return_policy_templates.view', 'View Return Policy Templates', 'View platform return policy templates.', 'ACTIVE', now(), now()),
                    ('62000000-0000-0000-0000-000000000033', 'platform.return_policy_templates.create', 'Create Return Policy Templates', 'Create platform return policy templates.', 'ACTIVE', now(), now()),
                    ('62000000-0000-0000-0000-000000000034', 'platform.return_policy_templates.update', 'Update Return Policy Templates', 'Update platform return policy templates.', 'ACTIVE', now(), now()),
                    ('62000000-0000-0000-0000-000000000035', 'platform.return_policy_templates.delete', 'Delete Return Policy Templates', 'Delete platform return policy templates.', 'ACTIVE', now(), now()),
                    ('62000000-0000-0000-0000-000000000036', 'platform.return_policy_templates.manage', 'Manage Return Policy Templates', 'Manage platform return policy templates.', 'ACTIVE', now(), now())
                ON CONFLICT (permission_code) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    status = 'ACTIVE',
                    updated_at = now();

                INSERT INTO platform_role_permissions (id, platform_role_id, platform_permission_id, description, created_at, updated_at)
                SELECT md5(role.role_code || ':' || permission.permission_code)::uuid,
                       role.id,
                       permission.id,
                       'Return policy template permission seed.',
                       now(),
                       now()
                FROM platform_roles role
                JOIN platform_permissions permission ON permission.permission_code IN (
                    'platform.return_policy_templates.view',
                    'platform.return_policy_templates.create',
                    'platform.return_policy_templates.update',
                    'platform.return_policy_templates.delete',
                    'platform.return_policy_templates.manage')
                WHERE role.role_code = 'super_administrator'
                ON CONFLICT (platform_role_id, platform_permission_id) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0141-4000-8000-000000000001', 'catalog.return_policies.view', 'View Return Policies', 'ACTIVE', now(), now()),
                    ('77777777-0142-4000-8000-000000000001', 'catalog.return_policies.create', 'Create Return Policies', 'ACTIVE', now(), now()),
                    ('77777777-0143-4000-8000-000000000001', 'catalog.return_policies.update', 'Update Return Policies', 'ACTIVE', now(), now()),
                    ('77777777-0144-4000-8000-000000000001', 'catalog.return_policies.delete', 'Delete Return Policies', 'ACTIVE', now(), now()),
                    ('77777777-0145-4000-8000-000000000001', 'catalog.return_policies.manage', 'Manage Return Policies', 'ACTIVE', now(), now())
                ON CONFLICT (permission_code) DO UPDATE
                SET name = EXCLUDED.name,
                    status = 'ACTIVE',
                    updated_at = now();

                INSERT INTO tenant_role_permissions (id, tenant_role_id, permission_definition_id, description, created_at, updated_at)
                SELECT md5(mapping.role_code || ':' || mapping.permission_code)::uuid,
                       tenant_roles.id,
                       permission_definitions.id,
                       'Development return policy permission seed.',
                       now(),
                       now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'catalog.return_policies.view'),
                        ('TENANT_ADMIN', 'catalog.return_policies.create'),
                        ('TENANT_ADMIN', 'catalog.return_policies.update'),
                        ('TENANT_ADMIN', 'catalog.return_policies.delete'),
                        ('TENANT_ADMIN', 'catalog.return_policies.manage'),
                        ('STORE_MANAGER', 'catalog.return_policies.view'),
                        ('STORE_MANAGER', 'catalog.return_policies.manage'),
                        ('INVENTORY_MANAGER', 'catalog.return_policies.view')
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
                      'catalog.return_policies.view',
                      'catalog.return_policies.create',
                      'catalog.return_policies.update',
                      'catalog.return_policies.delete',
                      'catalog.return_policies.manage'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'catalog.return_policies.view',
                    'catalog.return_policies.create',
                    'catalog.return_policies.update',
                    'catalog.return_policies.delete',
                    'catalog.return_policies.manage'
                );

                DELETE FROM platform_role_permissions
                USING platform_permissions
                WHERE platform_role_permissions.platform_permission_id = platform_permissions.id
                  AND platform_permissions.permission_code IN (
                      'platform.return_policy_templates.view',
                      'platform.return_policy_templates.create',
                      'platform.return_policy_templates.update',
                      'platform.return_policy_templates.delete',
                      'platform.return_policy_templates.manage'
                  );

                DELETE FROM platform_permissions
                WHERE permission_code IN (
                    'platform.return_policy_templates.view',
                    'platform.return_policy_templates.create',
                    'platform.return_policy_templates.update',
                    'platform.return_policy_templates.delete',
                    'platform.return_policy_templates.manage'
                );
                """);

            migrationBuilder.DropTable(
                name: "return_policy_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_policies_status",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "status",
                table: "return_policies");
        }
    }
}