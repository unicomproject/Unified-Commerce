using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260823091500_ReconcileTenantRoleSetupCatalog")]
public partial class ReconcileTenantRoleSetupCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO platform_modules (
                id, module_code, module_key, module_name, name, description,
                status, sort_order, is_core_module, created_at, updated_at)
            VALUES
                ('71100000-0000-0000-0000-000000000001', 'dashboard', 'dashboard', 'Dashboard', 'Dashboard', 'Business overview and insights.', 'ACTIVE', 10, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000002', 'outlets', 'outlets', 'Outlets', 'Outlets', 'Outlet management and information.', 'ACTIVE', 20, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000003', 'tills', 'tills', 'Tills', 'Tills', 'Till configuration and monitoring.', 'ACTIVE', 30, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000004', 'users', 'users', 'Users', 'Users', 'Tenant users and role access.', 'ACTIVE', 40, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000005', 'products', 'products', 'Products', 'Products', 'Products, categories, brands, and catalog.', 'ACTIVE', 50, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000006', 'inventory', 'inventory', 'Inventory', 'Inventory', 'Stock management and operations.', 'ACTIVE', 60, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000007', 'sales_pos', 'sales_pos', 'Sales (POS)', 'Sales (POS)', 'POS checkout and cashier operations.', 'ACTIVE', 70, TRUE, now(), now()),
                ('71100000-0000-0000-0000-000000000008', 'reports', 'reports', 'Reports', 'Reports', 'Business and operational reports.', 'ACTIVE', 80, TRUE, now(), now())
            ON CONFLICT (module_code) DO UPDATE
            SET module_key = EXCLUDED.module_key,
                module_name = EXCLUDED.module_name,
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = 'ACTIVE',
                sort_order = EXCLUDED.sort_order,
                updated_at = now();

            UPDATE platform_modules
            SET module_key = 'online_store',
                module_name = 'Online Store',
                name = 'Online Store',
                description = 'Manage online store and fulfillment.',
                status = 'ACTIVE',
                sort_order = 90,
                updated_at = now()
            WHERE module_code = 'online_store';

            WITH classified_permissions AS (
                SELECT
                    permission_definitions.id,
                    CASE
                        WHEN permission_definitions.permission_code LIKE 'tenant.dashboard.%' THEN 'dashboard'
                        WHEN permission_definitions.permission_code LIKE 'tenant.outlets.%' THEN 'outlets'
                        WHEN permission_definitions.permission_code LIKE 'tenant.tills.%'
                            OR permission_definitions.permission_code LIKE 'tenant.till.%' THEN 'tills'
                        WHEN permission_definitions.permission_code LIKE 'tenant.users.%'
                            OR permission_definitions.permission_code LIKE 'tenant.roles.%'
                            OR permission_definitions.permission_code LIKE 'roles.%' THEN 'users'
                        WHEN permission_definitions.permission_code LIKE 'catalog.%'
                            OR permission_definitions.permission_code LIKE 'products.%'
                            OR permission_definitions.permission_code LIKE 'product.%'
                            OR permission_definitions.permission_code LIKE 'categories.%'
                            OR permission_definitions.permission_code LIKE 'brands.%' THEN 'products'
                        WHEN permission_definitions.permission_code LIKE 'inventory.%'
                            OR permission_definitions.permission_code LIKE 'stock.%' THEN 'inventory'
                        WHEN permission_definitions.permission_code LIKE 'reports.%' THEN 'reports'
                        WHEN permission_definitions.permission_code LIKE 'tenant.online_store.%'
                            OR permission_definitions.permission_code LIKE 'online_store.%'
                            OR permission_definitions.permission_code LIKE 'fulfillment.%' THEN 'online_store'
                        WHEN permission_definitions.permission_code LIKE 'pos.%'
                            OR permission_definitions.permission_code LIKE 'sales.%'
                            OR permission_definitions.permission_code LIKE 'payments.%'
                            OR permission_definitions.permission_code LIKE 'receipt.%'
                            OR permission_definitions.permission_code LIKE 'returns.%'
                            OR permission_definitions.permission_code LIKE 'refunds.%'
                            OR permission_definitions.permission_code LIKE 'exchanges.%'
                            OR permission_definitions.permission_code LIKE 'customers.%'
                            OR permission_definitions.permission_code LIKE 'cash_drawer.%'
                            OR permission_definitions.permission_code = 'orders.view' THEN 'sales_pos'
                    END AS module_code
                FROM permission_definitions
                WHERE permission_definitions.is_active
                  AND permission_definitions.permission_code NOT LIKE 'platform.%'
            )
            UPDATE permission_definitions
            SET module_id = platform_modules.id,
                updated_at = now()
            FROM classified_permissions
            JOIN platform_modules ON platform_modules.module_code = classified_permissions.module_code
            WHERE permission_definitions.id = classified_permissions.id
              AND classified_permissions.module_code IS NOT NULL
              AND permission_definitions.module_id <> platform_modules.id;

            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
                granted_at, notes, created_at)
            SELECT
                md5(tenant_roles.id::text || ':' || permission_definitions.id::text)::uuid,
                tenant_roles.tenant_id,
                tenant_roles.id,
                permission_definitions.id,
                NULL,
                now(),
                'R1 Cashier delegation ceiling reconciliation.',
                now()
            FROM tenant_roles
            JOIN tenant_feature_entitlements
                ON tenant_feature_entitlements.tenant_id = tenant_roles.tenant_id
            JOIN platform_features entitlement_feature
                ON entitlement_feature.id = tenant_feature_entitlements.platform_feature_id
            JOIN permission_definitions
                ON permission_definitions.permission_code IN (
                    'cash_drawer.manage', 'cash_drawer.movement.create', 'cash_drawer.view',
                    'customers.create', 'customers.update', 'customers.view',
                    'exchanges.create', 'exchanges.view', 'orders.view',
                    'payments.cash.accept', 'pos.hardware.settings', 'pos.home.view',
                    'pos.home.view_dashboard', 'pos.new_sale.view', 'pos.notifications.view',
                    'pos.till.close', 'pos.till.open', 'pos.till.view_session',
                    'products.search', 'products.view', 'receipt.print', 'receipt.reprint',
                    'receipt.view', 'refunds.create', 'refunds.view', 'returns.create',
                    'returns.view', 'sales.cart.add_item', 'sales.cart.clear',
                    'sales.cart.manage', 'sales.cart.remove_item', 'sales.cart.update_item',
                    'sales.checkout', 'sales.create', 'sales.discount.apply',
                    'sales.park.create', 'sales.park.recall', 'sales.park.view',
                    'sales.view', 'tenant.till.manage', 'tenant.tills.manage'
                )
            JOIN platform_features permission_feature
                ON permission_feature.id = permission_definitions.feature_id
            JOIN platform_modules permission_module
                ON permission_module.id = permission_definitions.module_id
            WHERE tenant_roles.role_code = 'TENANT_ADMIN'
              AND tenant_roles.is_active
              AND entitlement_feature.feature_code = 'pos_checkout'
              AND tenant_feature_entitlements.entitlement_status = 'ENABLED'
              AND tenant_feature_entitlements.is_enabled
              AND tenant_feature_entitlements.revoked_at IS NULL
              AND tenant_feature_entitlements.effective_from <= now()
              AND (tenant_feature_entitlements.effective_until IS NULL
                   OR tenant_feature_entitlements.effective_until > now())
              AND permission_definitions.is_active
              AND permission_feature.status = 'ACTIVE'
              AND permission_module.status = 'ACTIVE'
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
