using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260701110500_SeedTenantLoginUsers")]
    public partial class SeedTenantLoginUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string passwordHash = "PBKDF2-SHA256:100000:zG7O+AY1EJBG5+sCXDBinA==:weI+nABmBRNW19gQODOHn5D2q8SUQ0rVJy0NITO/Qyo=";

            migrationBuilder.Sql("""
                INSERT INTO currencies (id, currency_code, name, decimal_places, created_at, updated_at)
                VALUES ('44444444-0001-4000-8000-000000000001', 'LKR', 'Sri Lankan Rupee', 2, now(), now())
                ON CONFLICT (currency_code) DO UPDATE
                SET name = EXCLUDED.name,
                    decimal_places = EXCLUDED.decimal_places,
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO business_types (id, business_type_code, name, description, status, created_at, updated_at)
                VALUES (
                    '44444444-0002-4000-8000-000000000001',
                    'RETAIL',
                    'Retail',
                    'Development retail tenant seed business type.',
                    'ACTIVE',
                    now(),
                    now()
                )
                ON CONFLICT (business_type_code) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO tenants (
                    id, tenant_code, currency_code, name, status, base_currency, billing_status,
                    business_type, business_type_id, default_locale, default_timezone, operating_mode,
                    primary_domain, created_at, updated_at
                )
                VALUES (
                    '55555555-0000-4000-8000-000000000001',
                    'DEV-TENANT-001',
                    'LKR',
                    'TM-EPOS Development Tenant',
                    'active',
                    'LKR',
                    'paid',
                    'Retail',
                    '44444444-0002-4000-8000-000000000001',
                    'en-LK',
                    'Asia/Colombo',
                    'unified_epos',
                    'dev-tenant.local',
                    now(),
                    now()
                )
                ON CONFLICT (tenant_code) DO UPDATE
                SET name = EXCLUDED.name,
                    status = 'active',
                    billing_status = 'paid',
                    business_type_id = EXCLUDED.business_type_id,
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO role_templates (id, template_code, name, description, status, sort_order, created_at, updated_at)
                VALUES (
                    '66666666-0000-4000-8000-000000000001',
                    'dev.tenant.roles',
                    'Development Tenant Roles',
                    'Seed role template for development tenant users.',
                    'ACTIVE',
                    1,
                    now(),
                    now()
                )
                ON CONFLICT (template_code) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO role_template_versions (id, role_template_id, version_number, created_at, updated_at)
                VALUES (
                    '66666666-0001-4000-8000-000000000001',
                    '66666666-0000-4000-8000-000000000001',
                    1,
                    now(),
                    now()
                )
                ON CONFLICT (role_template_id, version_number) DO UPDATE
                SET updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO permission_definitions (id, permission_code, name, status, created_at, updated_at)
                VALUES
                    ('77777777-0001-4000-8000-000000000001', 'tenant.dashboard.view', 'View Tenant Dashboard', 'ACTIVE', now(), now()),
                    ('77777777-0002-4000-8000-000000000001', 'tenant.settings.manage', 'Manage Tenant Settings', 'ACTIVE', now(), now()),
                    ('77777777-0003-4000-8000-000000000001', 'tenant.users.manage', 'Manage Tenant Users', 'ACTIVE', now(), now()),
                    ('77777777-0004-4000-8000-000000000001', 'tenant.roles.manage', 'Manage Tenant Roles', 'ACTIVE', now(), now()),
                    ('77777777-0005-4000-8000-000000000001', 'tenant.outlets.manage', 'Manage Outlets', 'ACTIVE', now(), now()),
                    ('77777777-0006-4000-8000-000000000001', 'catalog.products.view', 'View Products', 'ACTIVE', now(), now()),
                    ('77777777-0007-4000-8000-000000000001', 'catalog.products.create', 'Create Products', 'ACTIVE', now(), now()),
                    ('77777777-0008-4000-8000-000000000001', 'catalog.products.update', 'Update Products', 'ACTIVE', now(), now()),
                    ('77777777-0009-4000-8000-000000000001', 'inventory.stock.view', 'View Stock', 'ACTIVE', now(), now()),
                    ('77777777-0010-4000-8000-000000000001', 'pos.sale.create', 'Create POS Sale', 'ACTIVE', now(), now()),
                    ('77777777-0011-4000-8000-000000000001', 'pos.till.open', 'Open Till', 'ACTIVE', now(), now()),
                    ('77777777-0012-4000-8000-000000000001', 'pos.till.close', 'Close Till', 'ACTIVE', now(), now()),
                    ('77777777-0013-4000-8000-000000000001', 'fulfillment.orders.view', 'View Fulfilment Orders', 'ACTIVE', now(), now()),
                    ('77777777-0014-4000-8000-000000000001', 'fulfillment.orders.manage', 'Manage Fulfilment Orders', 'ACTIVE', now(), now()),
                    ('77777777-0015-4000-8000-000000000001', 'reports.sales.view', 'View Sales Reports', 'ACTIVE', now(), now())
                ON CONFLICT (permission_code) DO UPDATE
                SET name = EXCLUDED.name,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO tenant_roles (id, tenant_id, role_code, name, description, status, role_template_version_id, created_at, updated_at)
                VALUES
                    ('88888888-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'TENANT_ADMIN', 'Tenant Admin', 'Development tenant administrator.', 'ACTIVE', '66666666-0001-4000-8000-000000000001', now(), now()),
                    ('88888888-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'STORE_MANAGER', 'Store Manager', 'Development store manager.', 'ACTIVE', '66666666-0001-4000-8000-000000000001', now(), now()),
                    ('88888888-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CASHIER', 'Cashier', 'Development POS cashier.', 'ACTIVE', '66666666-0001-4000-8000-000000000001', now(), now()),
                    ('88888888-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'INVENTORY_MANAGER', 'Inventory Manager', 'Development inventory manager.', 'ACTIVE', '66666666-0001-4000-8000-000000000001', now(), now()),
                    ('88888888-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'FULFILLMENT_STAFF', 'Fulfilment Staff', 'Development fulfilment staff.', 'ACTIVE', '66666666-0001-4000-8000-000000000001', now(), now())
                ON CONFLICT (tenant_id, role_code) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql($"""
                INSERT INTO tenant_users (id, tenant_id, normalized_email, normalized_phone, password_hash, status, created_at, updated_at)
                VALUES
                    ('99999999-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'TENANTADMIN001@GMAIL.COM', NULL, '{passwordHash}', 'ACTIVE', now(), now()),
                    ('99999999-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'STOREMANAGER001@GMAIL.COM', NULL, '{passwordHash}', 'ACTIVE', now(), now()),
                    ('99999999-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CASHIER001@GMAIL.COM', NULL, '{passwordHash}', 'ACTIVE', now(), now()),
                    ('99999999-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'INVENTORY001@GMAIL.COM', NULL, '{passwordHash}', 'ACTIVE', now(), now()),
                    ('99999999-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'FULFILLMENT001@GMAIL.COM', NULL, '{passwordHash}', 'ACTIVE', now(), now())
                ON CONFLICT (normalized_email) DO UPDATE
                SET tenant_id = EXCLUDED.tenant_id,
                    password_hash = EXCLUDED.password_hash,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO tenant_user_roles (id, tenant_user_id, tenant_role_id, description, created_at, updated_at)
                VALUES
                    ('aaaaaaaa-0001-4000-8000-000000000001', '99999999-0001-4000-8000-000000000001', '88888888-0001-4000-8000-000000000001', 'Development tenant admin role assignment.', now(), now()),
                    ('aaaaaaaa-0002-4000-8000-000000000001', '99999999-0002-4000-8000-000000000001', '88888888-0002-4000-8000-000000000001', 'Development store manager role assignment.', now(), now()),
                    ('aaaaaaaa-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', '88888888-0003-4000-8000-000000000001', 'Development cashier role assignment.', now(), now()),
                    ('aaaaaaaa-0004-4000-8000-000000000001', '99999999-0004-4000-8000-000000000001', '88888888-0004-4000-8000-000000000001', 'Development inventory manager role assignment.', now(), now()),
                    ('aaaaaaaa-0005-4000-8000-000000000001', '99999999-0005-4000-8000-000000000001', '88888888-0005-4000-8000-000000000001', 'Development fulfilment staff role assignment.', now(), now())
                ON CONFLICT (tenant_user_id, tenant_role_id) DO UPDATE
                SET description = EXCLUDED.description,
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO tenant_role_permissions (id, tenant_role_id, permission_definition_id, description, created_at, updated_at)
                SELECT md5(mapping.role_code || ':' || mapping.permission_code)::uuid, tenant_roles.id, permission_definitions.id, 'Development role permission seed.', now(), now()
                FROM (
                    VALUES
                        ('TENANT_ADMIN', 'tenant.dashboard.view'),
                        ('TENANT_ADMIN', 'tenant.settings.manage'),
                        ('TENANT_ADMIN', 'tenant.users.manage'),
                        ('TENANT_ADMIN', 'tenant.roles.manage'),
                        ('TENANT_ADMIN', 'tenant.outlets.manage'),
                        ('TENANT_ADMIN', 'catalog.products.view'),
                        ('TENANT_ADMIN', 'catalog.products.create'),
                        ('TENANT_ADMIN', 'catalog.products.update'),
                        ('TENANT_ADMIN', 'inventory.stock.view'),
                        ('TENANT_ADMIN', 'reports.sales.view'),
                        ('STORE_MANAGER', 'tenant.dashboard.view'),
                        ('STORE_MANAGER', 'catalog.products.view'),
                        ('STORE_MANAGER', 'inventory.stock.view'),
                        ('STORE_MANAGER', 'pos.sale.create'),
                        ('STORE_MANAGER', 'pos.till.open'),
                        ('STORE_MANAGER', 'pos.till.close'),
                        ('STORE_MANAGER', 'reports.sales.view'),
                        ('CASHIER', 'tenant.dashboard.view'),
                        ('CASHIER', 'pos.sale.create'),
                        ('CASHIER', 'pos.till.open'),
                        ('CASHIER', 'pos.till.close'),
                        ('INVENTORY_MANAGER', 'tenant.dashboard.view'),
                        ('INVENTORY_MANAGER', 'catalog.products.view'),
                        ('INVENTORY_MANAGER', 'inventory.stock.view'),
                        ('FULFILLMENT_STAFF', 'tenant.dashboard.view'),
                        ('FULFILLMENT_STAFF', 'fulfillment.orders.view'),
                        ('FULFILLMENT_STAFF', 'fulfillment.orders.manage')
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
                WHERE tenant_role_id IN (
                    '88888888-0001-4000-8000-000000000001',
                    '88888888-0002-4000-8000-000000000001',
                    '88888888-0003-4000-8000-000000000001',
                    '88888888-0004-4000-8000-000000000001',
                    '88888888-0005-4000-8000-000000000001'
                );

                DELETE FROM tenant_user_roles
                WHERE tenant_user_id IN (
                    '99999999-0001-4000-8000-000000000001',
                    '99999999-0002-4000-8000-000000000001',
                    '99999999-0003-4000-8000-000000000001',
                    '99999999-0004-4000-8000-000000000001',
                    '99999999-0005-4000-8000-000000000001'
                );

                DELETE FROM tenant_users
                WHERE id IN (
                    '99999999-0001-4000-8000-000000000001',
                    '99999999-0002-4000-8000-000000000001',
                    '99999999-0003-4000-8000-000000000001',
                    '99999999-0004-4000-8000-000000000001',
                    '99999999-0005-4000-8000-000000000001'
                );

                DELETE FROM tenant_roles
                WHERE id IN (
                    '88888888-0001-4000-8000-000000000001',
                    '88888888-0002-4000-8000-000000000001',
                    '88888888-0003-4000-8000-000000000001',
                    '88888888-0004-4000-8000-000000000001',
                    '88888888-0005-4000-8000-000000000001'
                );

                DELETE FROM role_template_versions
                WHERE id = '66666666-0001-4000-8000-000000000001';

                DELETE FROM role_templates
                WHERE id = '66666666-0000-4000-8000-000000000001';

                DELETE FROM tenants
                WHERE id = '55555555-0000-4000-8000-000000000001';

                DELETE FROM permission_definitions
                WHERE id::text LIKE '77777777-%';

                DELETE FROM business_types
                WHERE id = '44444444-0002-4000-8000-000000000001';

                DELETE FROM currencies
                WHERE id = '44444444-0001-4000-8000-000000000001';
                """);
        }
    }
}