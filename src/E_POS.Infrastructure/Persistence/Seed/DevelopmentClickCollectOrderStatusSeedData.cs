namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentClickCollectOrderStatusSeedData
{
    public const string UpSql = """
        -- Seed 10 development click-and-collect orders for customer order status testing.
        -- Customer-facing coverage: pending, accepted, preparing, ready, completed; 2 orders each.
        WITH seed_context AS (
            SELECT
                '55555555-0000-4000-8000-000000000001'::uuid AS tenant_id,
                customer.id AS customer_id,
                customer.display_name AS customer_name,
                customer.email AS customer_email,
                customer.phone AS customer_phone,
                outlet.id AS outlet_id,
                outlet.outlet_code AS outlet_code,
                outlet.outlet_name AS outlet_name,
                method_outlet.id AS fulfillment_method_outlet_id
            FROM customers customer
            JOIN outlets outlet
                ON outlet.tenant_id = customer.tenant_id
               AND outlet.id = 'bbbbbbbb-0001-4000-8000-000000000001'::uuid
            LEFT JOIN fulfillment_methods method
                ON method.tenant_id = customer.tenant_id
               AND method.method_code = 'CLICK_COLLECT'
            LEFT JOIN fulfillment_method_outlets method_outlet
                ON method_outlet.tenant_id = customer.tenant_id
               AND method_outlet.fulfillment_method_id = method.id
               AND method_outlet.outlet_id = outlet.id
            WHERE customer.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
              AND customer.normalized_email = 'CUSTOMER1@EXAMPLE.COM'
            LIMIT 1
        ),
        seed_orders AS (
            SELECT * FROM (
                VALUES
                    (1, 'e0000101-0001-4000-8000-000000000001'::uuid, 'ECOMM-SEED-PENDING-001',   'CONFIRMED', 'PENDING',              'UNPAID', 0.0000, 2500.0000, 2500.0000, 1.0000, 'cccc0004-0001-4000-8000-000000000001'::uuid, 'cccc0005-0001-4000-8000-000000000001'::uuid, 'Team Jersey',       'MER-001-SKU', 0.0000, 'ACTIVE',    interval '50 hours'),
                    (2, 'e0000101-0002-4000-8000-000000000001'::uuid, 'ECOMM-SEED-PENDING-002',   'CONFIRMED', 'PENDING',              'UNPAID', 0.0000, 3600.0000, 3600.0000, 2.0000, 'cccc0004-0002-4000-8000-000000000001'::uuid, 'cccc0005-0002-4000-8000-000000000001'::uuid, 'Training Jersey',   'MER-002-SKU', 0.0000, 'ACTIVE',    interval '48 hours'),
                    (3, 'e0000101-0003-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-001',  'ACCEPTED',  'ACCEPTED',             'UNPAID', 0.0000, 4200.0000, 4200.0000, 1.0000, 'cccc0004-0003-4000-8000-000000000001'::uuid, 'cccc0005-0003-4000-8000-000000000001'::uuid, 'Match Shorts',      'MER-003-SKU', 0.0000, 'ACTIVE',    interval '46 hours'),
                    (4, 'e0000101-0004-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-002',  'ACCEPTED',  'ACCEPTED',             'UNPAID', 0.0000, 1800.0000, 1800.0000, 1.0000, 'cccc0004-000c-4000-8000-000000000001'::uuid, 'cccc0005-000c-4000-8000-000000000001'::uuid, 'Training Basketball','MER-012-SKU', 0.0000, 'ACTIVE',    interval '44 hours'),
                    (5, 'e0000101-0005-4000-8000-000000000001'::uuid, 'ECOMM-SEED-PREPARING-001', 'ACCEPTED',  'PREPARING',            'UNPAID', 0.0000, 3200.0000, 3200.0000, 2.0000, 'cccc0004-0008-4000-8000-000000000001'::uuid, 'cccc0005-0008-4000-8000-000000000001'::uuid, 'Fan Scarf',         'MER-008-SKU', 0.0000, 'ACTIVE',    interval '42 hours'),
                    (6, 'e0000101-0006-4000-8000-000000000001'::uuid, 'ECOMM-SEED-PREPARING-002', 'ACCEPTED',  'PREPARING',            'UNPAID', 0.0000, 2800.0000, 2800.0000, 1.0000, 'cccc0004-000b-4000-8000-000000000001'::uuid, 'cccc0005-000b-4000-8000-000000000001'::uuid, 'Match Football',    'MER-011-SKU', 0.0000, 'ACTIVE',    interval '40 hours'),
                    (7, 'e0000101-0007-4000-8000-000000000001'::uuid, 'ECOMM-SEED-READY-001',     'ACCEPTED',  'READY_FOR_COLLECTION', 'UNPAID', 0.0000, 2200.0000, 2200.0000, 1.0000, 'cccc0004-000d-4000-8000-000000000001'::uuid, 'cccc0005-000d-4000-8000-000000000001'::uuid, 'Water Bottle',      'MER-013-SKU', 0.0000, 'ACTIVE',    interval '38 hours'),
                    (8, 'e0000101-0008-4000-8000-000000000001'::uuid, 'ECOMM-SEED-READY-002',     'ACCEPTED',  'READY_FOR_COLLECTION', 'UNPAID', 0.0000, 4500.0000, 4500.0000, 1.0000, 'cccc0004-000f-4000-8000-000000000001'::uuid, 'cccc0005-000f-4000-8000-000000000001'::uuid, 'Silicone Wristband','MER-015-SKU', 0.0000, 'ACTIVE',    interval '36 hours'),
                    (9, 'e0000101-0009-4000-8000-000000000001'::uuid, 'ECOMM-SEED-COMPLETED-001', 'COMPLETED', 'COLLECTED',            'PAID',   5500.0000, 5500.0000, 0.0000, 1.0000, 'cccc0010-0001-4000-8000-000000000001'::uuid, 'cccc0013-0001-4000-8000-000000000001'::uuid, 'Development Sneaker','DEV-SNEAKER-01', 1.0000, 'FULFILLED', interval '34 hours'),
                    (10,'e0000101-0010-4000-8000-000000000001'::uuid, 'ECOMM-SEED-COMPLETED-002', 'COMPLETED', 'COLLECTED',            'PAID',   6200.0000, 6200.0000, 0.0000, 1.0000, 'cccc0010-0001-4000-8000-000000000001'::uuid, 'cccc0013-0002-4000-8000-000000000001'::uuid, 'Development Sneaker','DEV-SNEAKER-02', 1.0000, 'FULFILLED', interval '32 hours')
            ) AS value(
                ordinal, id, order_number, order_status, fulfillment_status, payment_status,
                paid_amount, total_amount, balance_due, quantity, product_id, product_variant_id,
                product_name, sku, fulfilled_quantity, line_status, placed_age
            )
        )
        INSERT INTO sales_orders (
            id, tenant_id, order_status, order_number, paid_amount, total_amount, sales_channel_id,
            order_type, fulfillment_method_outlet_id, fulfillment_method_code_snapshot,
            requested_collection_at, requested_collection_end_at, collection_timezone_snapshot,
            business_date, reporting_outlet_id, reporting_outlet_code_snapshot, reporting_outlet_name_snapshot,
            currency_code, is_tax_included, subtotal_amount, discount_amount, tax_amount,
            charge_amount, rounding_amount, refunded_amount, balance_due,
            payment_status, fulfillment_status, customer_id, customer_name_snapshot,
            customer_email_snapshot, customer_phone_snapshot, placed_at, confirmed_at,
            completed_at, created_at, updated_at
        )
        SELECT
            seed_orders.id,
            seed_context.tenant_id,
            seed_orders.order_status,
            seed_orders.order_number,
            seed_orders.paid_amount,
            seed_orders.total_amount,
            'bbbbbbbb-000b-4000-8000-000000000001'::uuid,
            'CLICK_AND_COLLECT',
            seed_context.fulfillment_method_outlet_id,
            'CLICK_COLLECT',
            now() + interval '1 day',
            now() + interval '1 day 30 minutes',
            'Asia/Colombo',
            CURRENT_DATE,
            seed_context.outlet_id,
            seed_context.outlet_code,
            seed_context.outlet_name,
            'LKR',
            false,
            seed_orders.total_amount,
            0.0000,
            0.0000,
            0.0000,
            0.0000,
            0.0000,
            seed_orders.balance_due,
            seed_orders.payment_status,
            seed_orders.fulfillment_status,
            seed_context.customer_id,
            seed_context.customer_name,
            seed_context.customer_email,
            seed_context.customer_phone,
            now() - seed_orders.placed_age,
            CASE WHEN seed_orders.order_status IN ('ACCEPTED', 'COMPLETED') THEN now() - seed_orders.placed_age + interval '10 minutes' ELSE NULL END,
            CASE WHEN seed_orders.order_status = 'COMPLETED' THEN now() - seed_orders.placed_age + interval '90 minutes' ELSE NULL END,
            now() - seed_orders.placed_age,
            now()
        FROM seed_orders
        CROSS JOIN seed_context
        ON CONFLICT (id) DO UPDATE
        SET order_status = EXCLUDED.order_status,
            paid_amount = EXCLUDED.paid_amount,
            total_amount = EXCLUDED.total_amount,
            fulfillment_method_outlet_id = EXCLUDED.fulfillment_method_outlet_id,
            fulfillment_method_code_snapshot = EXCLUDED.fulfillment_method_code_snapshot,
            requested_collection_at = EXCLUDED.requested_collection_at,
            requested_collection_end_at = EXCLUDED.requested_collection_end_at,
            collection_timezone_snapshot = EXCLUDED.collection_timezone_snapshot,
            business_date = EXCLUDED.business_date,
            reporting_outlet_id = EXCLUDED.reporting_outlet_id,
            reporting_outlet_code_snapshot = EXCLUDED.reporting_outlet_code_snapshot,
            reporting_outlet_name_snapshot = EXCLUDED.reporting_outlet_name_snapshot,
            currency_code = EXCLUDED.currency_code,
            subtotal_amount = EXCLUDED.subtotal_amount,
            balance_due = EXCLUDED.balance_due,
            payment_status = EXCLUDED.payment_status,
            fulfillment_status = EXCLUDED.fulfillment_status,
            customer_id = EXCLUDED.customer_id,
            customer_name_snapshot = EXCLUDED.customer_name_snapshot,
            customer_email_snapshot = EXCLUDED.customer_email_snapshot,
            customer_phone_snapshot = EXCLUDED.customer_phone_snapshot,
            confirmed_at = EXCLUDED.confirmed_at,
            completed_at = EXCLUDED.completed_at,
            updated_at = now();

        WITH seed_orders AS (
            SELECT * FROM (
                VALUES
                    (1, 'e0000101-0001-4000-8000-000000000001'::uuid, 1.0000, 2500.0000, 0.0000, 'cccc0004-0001-4000-8000-000000000001'::uuid, 'cccc0005-0001-4000-8000-000000000001'::uuid, 'Team Jersey',        'MER-001-SKU',     0.0000, 'ACTIVE'),
                    (2, 'e0000101-0002-4000-8000-000000000001'::uuid, 2.0000, 1800.0000, 0.0000, 'cccc0004-0002-4000-8000-000000000001'::uuid, 'cccc0005-0002-4000-8000-000000000001'::uuid, 'Training Jersey',    'MER-002-SKU',     0.0000, 'ACTIVE'),
                    (3, 'e0000101-0003-4000-8000-000000000001'::uuid, 1.0000, 4200.0000, 0.0000, 'cccc0004-0003-4000-8000-000000000001'::uuid, 'cccc0005-0003-4000-8000-000000000001'::uuid, 'Match Shorts',       'MER-003-SKU',     0.0000, 'ACTIVE'),
                    (4, 'e0000101-0004-4000-8000-000000000001'::uuid, 1.0000, 1800.0000, 0.0000, 'cccc0004-000c-4000-8000-000000000001'::uuid, 'cccc0005-000c-4000-8000-000000000001'::uuid, 'Training Basketball','MER-012-SKU',     0.0000, 'ACTIVE'),
                    (5, 'e0000101-0005-4000-8000-000000000001'::uuid, 2.0000, 1600.0000, 0.0000, 'cccc0004-0008-4000-8000-000000000001'::uuid, 'cccc0005-0008-4000-8000-000000000001'::uuid, 'Fan Scarf',          'MER-008-SKU',     0.0000, 'ACTIVE'),
                    (6, 'e0000101-0006-4000-8000-000000000001'::uuid, 1.0000, 2800.0000, 0.0000, 'cccc0004-000b-4000-8000-000000000001'::uuid, 'cccc0005-000b-4000-8000-000000000001'::uuid, 'Match Football',     'MER-011-SKU',     0.0000, 'ACTIVE'),
                    (7, 'e0000101-0007-4000-8000-000000000001'::uuid, 1.0000, 2200.0000, 0.0000, 'cccc0004-000d-4000-8000-000000000001'::uuid, 'cccc0005-000d-4000-8000-000000000001'::uuid, 'Water Bottle',       'MER-013-SKU',     0.0000, 'ACTIVE'),
                    (8, 'e0000101-0008-4000-8000-000000000001'::uuid, 1.0000, 4500.0000, 0.0000, 'cccc0004-000f-4000-8000-000000000001'::uuid, 'cccc0005-000f-4000-8000-000000000001'::uuid, 'Silicone Wristband', 'MER-015-SKU',     0.0000, 'ACTIVE'),
                    (9, 'e0000101-0009-4000-8000-000000000001'::uuid, 1.0000, 5500.0000, 1.0000, 'cccc0010-0001-4000-8000-000000000001'::uuid, 'cccc0013-0001-4000-8000-000000000001'::uuid, 'Development Sneaker', 'DEV-SNEAKER-01',  1.0000, 'FULFILLED'),
                    (10,'e0000101-0010-4000-8000-000000000001'::uuid, 1.0000, 6200.0000, 1.0000, 'cccc0010-0001-4000-8000-000000000001'::uuid, 'cccc0013-0002-4000-8000-000000000001'::uuid, 'Development Sneaker', 'DEV-SNEAKER-02',  1.0000, 'FULFILLED')
            ) AS value(ordinal, order_id, quantity, unit_price, tax_amount, product_id, product_variant_id, product_name, sku, fulfilled_quantity, line_status)
        )
        INSERT INTO sales_order_lines (
            id, tenant_id, sales_order_id, line_number, product_id, product_variant_id,
            uom_id, sku_snapshot, product_name_snapshot, uom_code_snapshot, uom_name_snapshot,
            product_type_snapshot, product_structure_snapshot, quantity, original_unit_price,
            unit_price, line_subtotal_amount, line_discount_amount, line_tax_amount,
            line_total_amount, fulfilled_quantity, cancelled_quantity, returned_quantity,
            line_status, created_at, updated_at
        )
        SELECT
            format('e0000102-%s-4000-8000-000000000001', lpad(seed_orders.ordinal::text, 4, '0'))::uuid,
            '55555555-0000-4000-8000-000000000001'::uuid,
            seed_orders.order_id,
            1,
            seed_orders.product_id,
            seed_orders.product_variant_id,
            '91000000-0000-4000-8000-000000000001'::uuid,
            seed_orders.sku,
            seed_orders.product_name,
            'PCS',
            'Pieces',
            'STANDARD',
            'SIMPLE',
            seed_orders.quantity,
            seed_orders.unit_price,
            seed_orders.unit_price,
            seed_orders.quantity * seed_orders.unit_price,
            0.0000,
            seed_orders.tax_amount,
            (seed_orders.quantity * seed_orders.unit_price) + seed_orders.tax_amount,
            seed_orders.fulfilled_quantity,
            0.0000,
            0.0000,
            seed_orders.line_status,
            now(),
            now()
        FROM seed_orders
        ON CONFLICT (id) DO UPDATE
        SET product_id = EXCLUDED.product_id,
            product_variant_id = EXCLUDED.product_variant_id,
            sku_snapshot = EXCLUDED.sku_snapshot,
            product_name_snapshot = EXCLUDED.product_name_snapshot,
            quantity = EXCLUDED.quantity,
            original_unit_price = EXCLUDED.original_unit_price,
            unit_price = EXCLUDED.unit_price,
            line_subtotal_amount = EXCLUDED.line_subtotal_amount,
            line_tax_amount = EXCLUDED.line_tax_amount,
            line_total_amount = EXCLUDED.line_total_amount,
            fulfilled_quantity = EXCLUDED.fulfilled_quantity,
            line_status = EXCLUDED.line_status,
            updated_at = now();

        WITH history_rows AS (
            SELECT * FROM (
                VALUES
                    ('ECOMM-SEED-ACCEPTED-001',  1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-ACCEPTED-001',  2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-ACCEPTED-002',  1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-ACCEPTED-002',  2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-PREPARING-001', 1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-PREPARING-001', 2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-PREPARING-001', 3, 'FULFILLMENT_STATUS', 'ACCEPTED',             'PREPARING',            interval '30 minutes'),
                    ('ECOMM-SEED-PREPARING-002', 1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-PREPARING-002', 2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-PREPARING-002', 3, 'FULFILLMENT_STATUS', 'ACCEPTED',             'PREPARING',            interval '30 minutes'),
                    ('ECOMM-SEED-READY-001',     1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-READY-001',     2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-READY-001',     3, 'FULFILLMENT_STATUS', 'ACCEPTED',             'PREPARING',            interval '30 minutes'),
                    ('ECOMM-SEED-READY-001',     4, 'FULFILLMENT_STATUS', 'PREPARING',            'READY_FOR_COLLECTION', interval '70 minutes'),
                    ('ECOMM-SEED-READY-002',     1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-READY-002',     2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-READY-002',     3, 'FULFILLMENT_STATUS', 'ACCEPTED',             'PREPARING',            interval '30 minutes'),
                    ('ECOMM-SEED-READY-002',     4, 'FULFILLMENT_STATUS', 'PREPARING',            'READY_FOR_COLLECTION', interval '70 minutes'),
                    ('ECOMM-SEED-COMPLETED-001', 1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-COMPLETED-001', 2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-COMPLETED-001', 3, 'FULFILLMENT_STATUS', 'ACCEPTED',             'PREPARING',            interval '30 minutes'),
                    ('ECOMM-SEED-COMPLETED-001', 4, 'FULFILLMENT_STATUS', 'PREPARING',            'READY_FOR_COLLECTION', interval '70 minutes'),
                    ('ECOMM-SEED-COMPLETED-001', 5, 'ORDER_STATUS',       'ACCEPTED',             'COMPLETED',            interval '90 minutes'),
                    ('ECOMM-SEED-COMPLETED-001', 6, 'FULFILLMENT_STATUS', 'READY_FOR_COLLECTION', 'COLLECTED',            interval '90 minutes'),
                    ('ECOMM-SEED-COMPLETED-002', 1, 'ORDER_STATUS',       'CONFIRMED',            'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-COMPLETED-002', 2, 'FULFILLMENT_STATUS', 'PENDING',              'ACCEPTED',             interval '10 minutes'),
                    ('ECOMM-SEED-COMPLETED-002', 3, 'FULFILLMENT_STATUS', 'ACCEPTED',             'PREPARING',            interval '30 minutes'),
                    ('ECOMM-SEED-COMPLETED-002', 4, 'FULFILLMENT_STATUS', 'PREPARING',            'READY_FOR_COLLECTION', interval '70 minutes'),
                    ('ECOMM-SEED-COMPLETED-002', 5, 'ORDER_STATUS',       'ACCEPTED',             'COMPLETED',            interval '90 minutes'),
                    ('ECOMM-SEED-COMPLETED-002', 6, 'FULFILLMENT_STATUS', 'READY_FOR_COLLECTION', 'COLLECTED',            interval '90 minutes')
            ) AS value(order_number, sequence_number, status_type, old_status, new_status, changed_after)
        ),
        numbered_history AS (
            SELECT row_number() OVER (ORDER BY history_rows.order_number, history_rows.sequence_number) AS row_number, history_rows.*
            FROM history_rows
        )
        INSERT INTO sales_order_status_history (
            id, tenant_id, sales_order_id, sequence_number, status_type,
            old_status, new_status, changed_by_tenant_user_id, changed_at, change_reason
        )
        SELECT
            format('e0000103-%s-4000-8000-000000000001', lpad(numbered_history.row_number::text, 4, '0'))::uuid,
            sales_order.tenant_id,
            sales_order.id,
            numbered_history.sequence_number,
            numbered_history.status_type,
            numbered_history.old_status,
            numbered_history.new_status,
            NULL::uuid,
            COALESCE(sales_order.placed_at, sales_order.created_at) + numbered_history.changed_after,
            'Development seed click-and-collect order status lifecycle'
        FROM numbered_history
        JOIN sales_orders sales_order
            ON sales_order.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
           AND sales_order.order_number = numbered_history.order_number
        ON CONFLICT (tenant_id, sales_order_id, sequence_number) DO UPDATE
        SET status_type = EXCLUDED.status_type,
            old_status = EXCLUDED.old_status,
            new_status = EXCLUDED.new_status,
            changed_at = EXCLUDED.changed_at,
            change_reason = EXCLUDED.change_reason;
        """;

    public const string DownSql = """
        DELETE FROM sales_order_status_history
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND sales_order_id IN (
              SELECT id FROM sales_orders
              WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
                AND order_number LIKE 'ECOMM-SEED-%'
          );

        DELETE FROM sales_order_lines
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND sales_order_id IN (
              SELECT id FROM sales_orders
              WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
                AND order_number LIKE 'ECOMM-SEED-%'
          );

        DELETE FROM sales_orders
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND order_number LIKE 'ECOMM-SEED-%';
        """;
}