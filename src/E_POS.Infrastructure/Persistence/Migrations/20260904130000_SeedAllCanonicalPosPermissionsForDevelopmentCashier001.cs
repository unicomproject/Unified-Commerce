using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260904130000_SeedAllCanonicalPosPermissionsForDevelopmentCashier001")]
public partial class SeedAllCanonicalPosPermissionsForDevelopmentCashier001 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO platform_features (
                id, platform_module_id, feature_code, feature_key, feature_name,
                is_core_feature, name, description, status, sort_order, created_at, updated_at)
            VALUES
                (md5('canonical-pos-feature:pos.hardware')::uuid,
                 '71000000-0000-0000-0000-000000000010'::uuid,
                 'pos.hardware', 'pos.hardware', 'POS Hardware', TRUE,
                 'POS Hardware', 'POS local hardware-agent access.', 'ACTIVE', 90, now(), now()),
                (md5('canonical-pos-feature:pos.online_orders')::uuid,
                 '71000000-0000-0000-0000-000000000010'::uuid,
                 'pos.online_orders', 'pos.online_orders', 'POS Online Orders', TRUE,
                 'POS Online Orders', 'POS picking, packing, and collection operations.', 'ACTIVE', 95, now(), now())
            ON CONFLICT (feature_key) DO UPDATE
            SET status = 'ACTIVE', updated_at = now();

            WITH canonical(permission_code, feature_key, action_type) AS (
                VALUES
                    ('pos.sales.dashboard.view', 'pos.sales', 'view'),
                    ('pos.sales.new_sale.view', 'pos.sales', 'view'),
                    ('pos.sales.new_sale.create', 'pos.sales', 'create'),
                    ('pos.sales.catalog.view', 'pos.products', 'view'),
                    ('pos.sales.catalog.search', 'pos.products', 'search'),
                    ('pos.sales.cart.manage', 'pos.sales', 'manage'),
                    ('pos.sales.cart.add_item', 'pos.sales', 'add_item'),
                    ('pos.sales.cart.update_item', 'pos.sales', 'update_item'),
                    ('pos.sales.cart.remove_item', 'pos.sales', 'remove_item'),
                    ('pos.sales.cart.clear', 'pos.sales', 'clear'),
                    ('pos.sales.checkout.execute', 'pos.sales', 'execute'),
                    ('pos.sales.manual_discount.apply', 'pos.sales', 'apply'),
                    ('pos.sales.discount.approve', 'pos.sales', 'approve'),
                    ('pos.sales.held_sales.create', 'pos.sales', 'create'),
                    ('pos.sales.held_sales.view', 'pos.sales', 'view'),
                    ('pos.sales.held_sales.recall', 'pos.sales', 'recall'),
                    ('pos.sales.order_history.view', 'pos.sales', 'view'),
                    ('pos.payments.cash.accept', 'pos.payments', 'accept'),
                    ('pos.payments.card.accept', 'pos.payments', 'accept'),
                    ('pos.payments.qr.accept', 'pos.payments', 'accept'),
                    ('pos.payments.split.accept', 'pos.payments', 'accept'),
                    ('pos.receipts.digital.view', 'pos.receipts', 'view'),
                    ('pos.receipts.physical.print', 'pos.receipts', 'print'),
                    ('pos.receipts.history.reprint', 'pos.receipts', 'reprint'),
                    ('pos.orders.history.view', 'pos.orders', 'view'),
                    ('pos.customers.management.view', 'pos.customers', 'view'),
                    ('pos.customers.management.create', 'pos.customers', 'create'),
                    ('pos.customers.management.update', 'pos.customers', 'update'),
                    ('pos.returns.search_sale.view', 'pos.returns', 'view'),
                    ('pos.returns.workflow.create', 'pos.returns', 'create'),
                    ('pos.refunds.processing.view', 'pos.returns', 'view'),
                    ('pos.refunds.processing.create', 'pos.returns', 'create'),
                    ('pos.refunds.approval.approve', 'pos.returns', 'approve'),
                    ('pos.exchanges.processing.view', 'pos.exchanges', 'view'),
                    ('pos.exchanges.processing.create', 'pos.exchanges', 'create'),
                    ('pos.cash_drawer.position.view', 'pos.cash_drawer', 'view'),
                    ('pos.cash_drawer.physical.manage', 'pos.cash_drawer', 'manage'),
                    ('pos.cash_drawer.movements.create', 'pos.cash_drawer', 'create'),
                    ('pos.till.session.open', 'pos.till', 'open'),
                    ('pos.till.session.close', 'pos.till', 'close'),
                    ('pos.till.session.view', 'pos.till', 'view'),
                    ('pos.hardware.local_agent.settings', 'pos.hardware', 'settings'),
                    ('pos.notifications.alerts.view', 'pos.notifications', 'view'),
                    ('pos.online_orders.picking.view', 'pos.online_orders', 'view'),
                    ('pos.online_orders.picking.pick', 'pos.online_orders', 'pick'),
                    ('pos.online_orders.picking.scan', 'pos.online_orders', 'scan'),
                    ('pos.online_orders.picking.manual_entry', 'pos.online_orders', 'manual_entry'),
                    ('pos.online_orders.picking.report_issue', 'pos.online_orders', 'report_issue'),
                    ('pos.online_orders.packing.view', 'pos.online_orders', 'view'),
                    ('pos.online_orders.packing.pack', 'pos.online_orders', 'pack'),
                    ('pos.online_orders.collection.mark_ready', 'pos.online_orders', 'mark_ready'),
                    ('pos.online_orders.collection.notify', 'pos.online_orders', 'notify'),
                    ('pos.online_orders.collection.view', 'pos.online_orders', 'view'),
                    ('pos.online_orders.collection.scan_qr', 'pos.online_orders', 'scan_qr'),
                    ('pos.online_orders.collection.validate_qr', 'pos.online_orders', 'validate_qr'),
                    ('pos.online_orders.collection.lookup', 'pos.online_orders', 'lookup'),
                    ('pos.online_orders.collection.verify', 'pos.online_orders', 'verify'),
                    ('pos.online_orders.collection.handover', 'pos.online_orders', 'handover'),
                    ('pos.online_orders.collection.collect', 'pos.online_orders', 'collect'),
                    ('pos.online_orders.payment.retry', 'pos.online_orders', 'retry')
            )
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, created_at, updated_at)
            SELECT
                md5('canonical-pos-permission:' || canonical.permission_code)::uuid,
                canonical.permission_code,
                platform_features.platform_module_id,
                platform_features.id,
                canonical.action_type,
                'Canonical POS permission.',
                TRUE,
                TRUE,
                now(),
                now()
            FROM canonical
            JOIN platform_features ON platform_features.feature_key = canonical.feature_key
            ON CONFLICT (permission_code) DO UPDATE
            SET is_active = TRUE, updated_at = now();

            INSERT INTO tenant_user_permissions (
                id, tenant_id, user_id, permission_id, assigned_by_tenant_user_id,
                assigned_at, revoked_at, created_at)
            SELECT
                md5('development-cashier001-all-pos:' || pd.permission_code)::uuid,
                tu.tenant_id, tu.id, pd.id, NULL, now(), NULL, now()
            FROM tenant_users tu
            CROSS JOIN permission_definitions pd
            WHERE upper(tu.email) = 'CASHIER001@GMAIL.COM'
              AND tu.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
              AND tu.account_status = 'ACTIVE'
              AND pd.is_active = TRUE
              AND pd.permission_code LIKE 'pos.%'
            ON CONFLICT (tenant_id, user_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                assigned_at = COALESCE(tenant_user_permissions.assigned_at, EXCLUDED.assigned_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM tenant_user_permissions
            USING permission_definitions
            WHERE tenant_user_permissions.permission_id = permission_definitions.id
              AND permission_definitions.id =
                  md5('canonical-pos-permission:' || permission_definitions.permission_code)::uuid;

            DELETE FROM permission_definitions
            WHERE id = md5('canonical-pos-permission:' || permission_code)::uuid;

            DELETE FROM platform_features
            WHERE id IN (
                md5('canonical-pos-feature:pos.hardware')::uuid,
                md5('canonical-pos-feature:pos.online_orders')::uuid);
            """);
    }
}
