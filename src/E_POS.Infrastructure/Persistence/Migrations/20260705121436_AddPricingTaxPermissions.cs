using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTaxPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0201-4000-8000-000000000001', 'pricing.price_lists.view', 'View Price Lists', 'ACTIVE', now(), now()),
                    ('77777777-0202-4000-8000-000000000001', 'pricing.price_lists.create', 'Create Price Lists', 'ACTIVE', now(), now()),
                    ('77777777-0203-4000-8000-000000000001', 'pricing.price_lists.update', 'Update Price Lists', 'ACTIVE', now(), now()),
                    ('77777777-0204-4000-8000-000000000001', 'pricing.price_lists.delete', 'Delete Price Lists', 'ACTIVE', now(), now()),
                    ('77777777-0205-4000-8000-000000000001', 'pricing.price_lists.manage', 'Manage Price Lists', 'ACTIVE', now(), now()),
                    
                    ('77777777-0211-4000-8000-000000000001', 'tax.classes.view', 'View Tax Classes', 'ACTIVE', now(), now()),
                    ('77777777-0212-4000-8000-000000000001', 'tax.classes.create', 'Create Tax Classes', 'ACTIVE', now(), now()),
                    ('77777777-0213-4000-8000-000000000001', 'tax.classes.update', 'Update Tax Classes', 'ACTIVE', now(), now()),
                    ('77777777-0214-4000-8000-000000000001', 'tax.classes.delete', 'Delete Tax Classes', 'ACTIVE', now(), now()),
                    ('77777777-0215-4000-8000-000000000001', 'tax.classes.manage', 'Manage Tax Classes', 'ACTIVE', now(), now()),
                    
                    ('77777777-0221-4000-8000-000000000001', 'tax.rates.view', 'View Tax Rates', 'ACTIVE', now(), now()),
                    ('77777777-0222-4000-8000-000000000001', 'tax.rates.create', 'Create Tax Rates', 'ACTIVE', now(), now()),
                    ('77777777-0223-4000-8000-000000000001', 'tax.rates.update', 'Update Tax Rates', 'ACTIVE', now(), now()),
                    ('77777777-0224-4000-8000-000000000001', 'tax.rates.delete', 'Delete Tax Rates', 'ACTIVE', now(), now()),
                    ('77777777-0225-4000-8000-000000000001', 'tax.rates.manage', 'Manage Tax Rates', 'ACTIVE', now(), now())
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
                       'Development pricing and tax permission seed.',
                       now(),
                       now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'pricing.price_lists.view'),
                        ('TENANT_ADMIN', 'pricing.price_lists.create'),
                        ('TENANT_ADMIN', 'pricing.price_lists.update'),
                        ('TENANT_ADMIN', 'pricing.price_lists.delete'),
                        ('TENANT_ADMIN', 'pricing.price_lists.manage'),
                        ('TENANT_ADMIN', 'tax.classes.view'),
                        ('TENANT_ADMIN', 'tax.classes.create'),
                        ('TENANT_ADMIN', 'tax.classes.update'),
                        ('TENANT_ADMIN', 'tax.classes.delete'),
                        ('TENANT_ADMIN', 'tax.classes.manage'),
                        ('TENANT_ADMIN', 'tax.rates.view'),
                        ('TENANT_ADMIN', 'tax.rates.create'),
                        ('TENANT_ADMIN', 'tax.rates.update'),
                        ('TENANT_ADMIN', 'tax.rates.delete'),
                        ('TENANT_ADMIN', 'tax.rates.manage'),
                        
                        ('STORE_MANAGER', 'pricing.price_lists.view'),
                        ('STORE_MANAGER', 'pricing.price_lists.create'),
                        ('STORE_MANAGER', 'pricing.price_lists.update'),
                        ('STORE_MANAGER', 'pricing.price_lists.delete'),
                        ('STORE_MANAGER', 'pricing.price_lists.manage'),
                        ('STORE_MANAGER', 'tax.classes.view'),
                        ('STORE_MANAGER', 'tax.classes.manage'),
                        ('STORE_MANAGER', 'tax.rates.view'),
                        ('STORE_MANAGER', 'tax.rates.manage'),
                        
                        ('INVENTORY_MANAGER', 'pricing.price_lists.view')
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
                WHERE permission_definition_id IN (
                    SELECT id FROM permission_definitions 
                    WHERE permission_code LIKE 'pricing.price_lists%' OR permission_code LIKE 'tax.classes%' OR permission_code LIKE 'tax.rates%'
                );

                DELETE FROM permission_definitions 
                WHERE permission_code LIKE 'pricing.price_lists%' OR permission_code LIKE 'tax.classes%' OR permission_code LIKE 'tax.rates%';
                """);
        }
    }
}
