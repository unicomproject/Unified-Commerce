namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentSalesOrdersSeedData
{
    public const string UpSql = """
        -- 1. Insert 4 Sales Orders for Customer 1
        WITH customer AS (
            SELECT id FROM customers WHERE tenant_id = '55555555-0000-4000-8000-000000000001' LIMIT 1
        )
        INSERT INTO sales_orders (
            id, tenant_id, order_status, order_number, paid_amount, total_amount, sales_channel_id,
            order_type, currency_code, is_tax_included, subtotal_amount, discount_amount,
            tax_amount, charge_amount, rounding_amount, refunded_amount, balance_due,
            payment_status, fulfillment_status, customer_id, created_at, updated_at
        )
        SELECT v.id, v.tenant_id, v.order_status, v.order_number, v.paid_amount, v.total_amount, v.sales_channel_id,
            v.order_type, v.currency_code, v.is_tax_included, v.subtotal_amount, v.discount_amount,
            v.tax_amount, v.charge_amount, v.rounding_amount, v.refunded_amount, v.balance_due,
            v.payment_status, v.fulfillment_status, customer.id, v.created_at, v.updated_at
        FROM (
            VALUES
                ('e0000001-0001-4000-8000-000000000001'::uuid, '55555555-0000-4000-8000-000000000001'::uuid, 'CONFIRMED', 'ECOMM-001', 0, 100, 'bbbbbbbb-000b-4000-8000-000000000001'::uuid, 'CLICK_AND_COLLECT', 'USD', false, 100, 0, 0, 0, 0, 0, 100, 'UNPAID', 'PENDING', now(), now()),
                ('e0000001-0002-4000-8000-000000000001'::uuid, '55555555-0000-4000-8000-000000000001'::uuid, 'CONFIRMED', 'ECOMM-002', 100, 100, 'bbbbbbbb-000b-4000-8000-000000000001'::uuid, 'CLICK_AND_COLLECT', 'USD', false, 100, 0, 0, 0, 0, 0, 0, 'PAID', 'PARTIALLY_FULFILLED', now(), now()),
                ('e0000001-0003-4000-8000-000000000001'::uuid, '55555555-0000-4000-8000-000000000001'::uuid, 'CONFIRMED', 'ECOMM-003', 100, 100, 'bbbbbbbb-000b-4000-8000-000000000001'::uuid, 'CLICK_AND_COLLECT', 'USD', false, 100, 0, 0, 0, 0, 0, 0, 'PAID', 'READY_FOR_PICKUP', now(), now()),
                ('e0000001-0004-4000-8000-000000000001'::uuid, '55555555-0000-4000-8000-000000000001'::uuid, 'COMPLETED', 'ECOMM-004', 100, 100, 'bbbbbbbb-000b-4000-8000-000000000001'::uuid, 'CLICK_AND_COLLECT', 'USD', false, 100, 0, 0, 0, 0, 0, 0, 'PAID', 'FULFILLED', now(), now())
        ) AS v(id, tenant_id, order_status, order_number, paid_amount, total_amount, sales_channel_id, order_type, currency_code, is_tax_included, subtotal_amount, discount_amount, tax_amount, charge_amount, rounding_amount, refunded_amount, balance_due, payment_status, fulfillment_status, created_at, updated_at)
        CROSS JOIN customer
        ON CONFLICT (id) DO UPDATE
        SET order_status = EXCLUDED.order_status,
            fulfillment_status = EXCLUDED.fulfillment_status,
            updated_at = now();

        -- 2. Insert corresponding Sales Order Lines
        INSERT INTO sales_order_lines (
            id, tenant_id, sales_order_id, line_number, product_id, product_variant_id, 
            uom_id, product_name_snapshot, uom_code_snapshot, uom_name_snapshot,
            product_type_snapshot, product_structure_snapshot,
            quantity, original_unit_price, unit_price, line_subtotal_amount, 
            line_discount_amount, line_tax_amount, line_total_amount,
            fulfilled_quantity, cancelled_quantity, returned_quantity, line_status, created_at, updated_at
        )
        VALUES
            ('e0000002-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'e0000001-0001-4000-8000-000000000001', 1, 'cccc0010-0001-4000-8000-000000000001', 'cccc0013-0001-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'Development Sneaker', 'EA', 'Each', 'MERCHANDISE', 'VARIABLE', 1, 100, 100, 100, 0, 0, 100, 0, 0, 0, 'ACTIVE', now(), now()),
            ('e0000002-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'e0000001-0002-4000-8000-000000000001', 1, 'cccc0010-0001-4000-8000-000000000001', 'cccc0013-0001-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'Development Sneaker', 'EA', 'Each', 'MERCHANDISE', 'VARIABLE', 1, 100, 100, 100, 0, 0, 100, 0, 0, 0, 'ACTIVE', now(), now()),
            ('e0000002-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'e0000001-0003-4000-8000-000000000001', 1, 'cccc0010-0001-4000-8000-000000000001', 'cccc0013-0001-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'Development Sneaker', 'EA', 'Each', 'MERCHANDISE', 'VARIABLE', 1, 100, 100, 100, 0, 0, 100, 0, 0, 0, 'ACTIVE', now(), now()),
            ('e0000002-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'e0000001-0004-4000-8000-000000000001', 1, 'cccc0010-0001-4000-8000-000000000001', 'cccc0013-0001-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'Development Sneaker', 'EA', 'Each', 'MERCHANDISE', 'VARIABLE', 1, 100, 100, 100, 0, 0, 100, 1, 0, 0, 'ACTIVE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET fulfilled_quantity = EXCLUDED.fulfilled_quantity,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM sales_order_lines WHERE id IN ('e0000002-0001-4000-8000-000000000001', 'e0000002-0002-4000-8000-000000000001', 'e0000002-0003-4000-8000-000000000001', 'e0000002-0004-4000-8000-000000000001');
        DELETE FROM sales_orders WHERE id IN ('e0000001-0001-4000-8000-000000000001', 'e0000001-0002-4000-8000-000000000001', 'e0000001-0003-4000-8000-000000000001', 'e0000001-0004-4000-8000-000000000001');
        """;
}
