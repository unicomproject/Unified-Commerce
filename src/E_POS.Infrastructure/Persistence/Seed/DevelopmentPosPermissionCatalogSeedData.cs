using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosPermissionCatalogSeedData
{
    public const string UpSql = """
        INSERT INTO platform_modules (id, module_code, name, description, status, sort_order, created_at, updated_at)
        VALUES (
            '71000000-0000-0000-0000-000000000010',
            'core_pos',
            'Core POS',
            'POS home, selling, checkout, and cashier operations.',
            'ACTIVE',
            10,
            now(),
            now()
        )
        ON CONFLICT (module_code) DO UPDATE
        SET name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            updated_at = now();

        INSERT INTO platform_features (
            id,
            platform_module_id,
            feature_code,
            feature_key,
            feature_name,
            is_core_feature,
            name,
            description,
            status,
            sort_order,
            created_at,
            updated_at)
        VALUES
            ('72000000-0000-0000-0000-000000000010', '71000000-0000-0000-0000-000000000010', 'pos.home', 'pos.home', 'POS Home', true, 'POS Home', 'Cashier home dashboard and navigation.', 'ACTIVE', 10, now(), now()),
            ('72000000-0000-0000-0000-000000000011', '71000000-0000-0000-0000-000000000010', 'pos.sales', 'pos.sales', 'POS Sales', true, 'POS Sales', 'New sale, cart, checkout, and parked sales.', 'ACTIVE', 20, now(), now()),
            ('72000000-0000-0000-0000-000000000012', '71000000-0000-0000-0000-000000000010', 'pos.products', 'pos.products', 'POS Products', true, 'POS Products', 'Product grid and search on POS.', 'ACTIVE', 30, now(), now()),
            ('72000000-0000-0000-0000-000000000013', '71000000-0000-0000-0000-000000000010', 'pos.customers', 'pos.customers', 'POS Customers', true, 'POS Customers', 'Customer lookup and creation on POS.', 'ACTIVE', 40, now(), now()),
            ('72000000-0000-0000-0000-000000000014', '71000000-0000-0000-0000-000000000010', 'pos.payments', 'pos.payments', 'POS Payments', true, 'POS Payments', 'Payment method capture on POS.', 'ACTIVE', 50, now(), now()),
            ('72000000-0000-0000-0000-000000000015', '71000000-0000-0000-0000-000000000010', 'pos.receipts', 'pos.receipts', 'POS Receipts', true, 'POS Receipts', 'Receipt view and print on POS.', 'ACTIVE', 60, now(), now()),
            ('72000000-0000-0000-0000-000000000016', '71000000-0000-0000-0000-000000000010', 'pos.orders', 'pos.orders', 'POS Orders', true, 'POS Orders', 'Orders sidebar and order visibility on POS.', 'ACTIVE', 70, now(), now()),
            ('72000000-0000-0000-0000-000000000017', '71000000-0000-0000-0000-000000000010', 'pos.returns', 'pos.returns', 'POS Returns', true, 'POS Returns', 'Returns and refunds on POS.', 'ACTIVE', 80, now(), now()),
            ('72000000-0000-0000-0000-000000000018', '71000000-0000-0000-0000-000000000010', 'pos.cash_drawer', 'pos.cash_drawer', 'POS Cash Drawer', true, 'POS Cash Drawer', 'Cash drawer summary and movements.', 'ACTIVE', 90, now(), now()),
            ('72000000-0000-0000-0000-000000000019', '71000000-0000-0000-0000-000000000010', 'pos.till', 'pos.till', 'POS Till', true, 'POS Till', 'Till session open/close and status.', 'ACTIVE', 100, now(), now()),
            ('72000000-0000-0000-0000-000000000020', '71000000-0000-0000-0000-000000000010', 'pos.notifications', 'pos.notifications', 'POS Notifications', true, 'POS Notifications', 'Cashier notification bell.', 'ACTIVE', 110, now(), now()),
            ('72000000-0000-0000-0000-000000000021', '71000000-0000-0000-0000-000000000010', 'tenant.till_ops', 'tenant.till_ops', 'Tenant Till Operations', true, 'Tenant Till Operations', 'Till management for device activation.', 'ACTIVE', 120, now(), now())
        ON CONFLICT (feature_key) DO UPDATE
        SET platform_module_id = EXCLUDED.platform_module_id,
            feature_code = EXCLUDED.feature_code,
            feature_name = EXCLUDED.feature_name,
            is_core_feature = EXCLUDED.is_core_feature,
            name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM platform_features
        WHERE id IN (
            '72000000-0000-0000-0000-000000000010',
            '72000000-0000-0000-0000-000000000011',
            '72000000-0000-0000-0000-000000000012',
            '72000000-0000-0000-0000-000000000013',
            '72000000-0000-0000-0000-000000000014',
            '72000000-0000-0000-0000-000000000015',
            '72000000-0000-0000-0000-000000000016',
            '72000000-0000-0000-0000-000000000017',
            '72000000-0000-0000-0000-000000000018',
            '72000000-0000-0000-0000-000000000019',
            '72000000-0000-0000-0000-000000000020',
            '72000000-0000-0000-0000-000000000021'
        );

        DELETE FROM platform_modules
        WHERE id = '71000000-0000-0000-0000-000000000010';
        """;
}
