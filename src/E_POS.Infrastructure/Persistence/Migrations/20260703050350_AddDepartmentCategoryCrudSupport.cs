using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentCategoryCrudSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "parent_category_id",
                table: "categories",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0101-4000-8000-000000000001', 'catalog.departments.view', 'View Departments', 'ACTIVE', now(), now()),
                    ('77777777-0102-4000-8000-000000000001', 'catalog.departments.create', 'Create Departments', 'ACTIVE', now(), now()),
                    ('77777777-0103-4000-8000-000000000001', 'catalog.departments.update', 'Update Departments', 'ACTIVE', now(), now()),
                    ('77777777-0104-4000-8000-000000000001', 'catalog.departments.delete', 'Delete Departments', 'ACTIVE', now(), now()),
                    ('77777777-0105-4000-8000-000000000001', 'catalog.departments.manage', 'Manage Departments', 'ACTIVE', now(), now()),
                    ('77777777-0111-4000-8000-000000000001', 'catalog.categories.view', 'View Categories', 'ACTIVE', now(), now()),
                    ('77777777-0112-4000-8000-000000000001', 'catalog.categories.create', 'Create Categories', 'ACTIVE', now(), now()),
                    ('77777777-0113-4000-8000-000000000001', 'catalog.categories.update', 'Update Categories', 'ACTIVE', now(), now()),
                    ('77777777-0114-4000-8000-000000000001', 'catalog.categories.delete', 'Delete Categories', 'ACTIVE', now(), now()),
                    ('77777777-0115-4000-8000-000000000001', 'catalog.categories.manage', 'Manage Categories', 'ACTIVE', now(), now())
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
                       'Development catalog master data permission seed.',
                       now(),
                       now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'catalog.departments.view'),
                        ('TENANT_ADMIN', 'catalog.departments.create'),
                        ('TENANT_ADMIN', 'catalog.departments.update'),
                        ('TENANT_ADMIN', 'catalog.departments.delete'),
                        ('TENANT_ADMIN', 'catalog.departments.manage'),
                        ('TENANT_ADMIN', 'catalog.categories.view'),
                        ('TENANT_ADMIN', 'catalog.categories.create'),
                        ('TENANT_ADMIN', 'catalog.categories.update'),
                        ('TENANT_ADMIN', 'catalog.categories.delete'),
                        ('TENANT_ADMIN', 'catalog.categories.manage'),
                        ('STORE_MANAGER', 'catalog.departments.view'),
                        ('STORE_MANAGER', 'catalog.departments.manage'),
                        ('STORE_MANAGER', 'catalog.categories.view'),
                        ('STORE_MANAGER', 'catalog.categories.manage'),
                        ('INVENTORY_MANAGER', 'catalog.departments.view'),
                        ('INVENTORY_MANAGER', 'catalog.categories.view')
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
                      'catalog.departments.view',
                      'catalog.departments.create',
                      'catalog.departments.update',
                      'catalog.departments.delete',
                      'catalog.departments.manage',
                      'catalog.categories.view',
                      'catalog.categories.create',
                      'catalog.categories.update',
                      'catalog.categories.delete',
                      'catalog.categories.manage'
                  );

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'catalog.departments.view',
                    'catalog.departments.create',
                    'catalog.departments.update',
                    'catalog.departments.delete',
                    'catalog.departments.manage',
                    'catalog.categories.view',
                    'catalog.categories.create',
                    'catalog.categories.update',
                    'catalog.categories.delete',
                    'catalog.categories.manage'
                );

                UPDATE categories
                SET parent_category_id = id
                WHERE parent_category_id IS NULL;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "parent_category_id",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
