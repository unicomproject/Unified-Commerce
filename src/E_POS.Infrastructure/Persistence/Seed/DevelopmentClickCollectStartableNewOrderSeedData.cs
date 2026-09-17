namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Deterministic Development repair so <c>ECOMM-SEED-PENDING-001</c> is a
/// startable New Click &amp; Collect order: future collection window, PENDING
/// sales projection, and the fulfilment / pickup / inventory graph required by
/// POS Start Fulfilment. Does not reset READY ACCEPTED fixtures used by later OO stages.
/// </summary>
public static class DevelopmentClickCollectStartableNewOrderSeedData
{
    public const string OrderNumber = "ECOMM-SEED-PENDING-001";
    public const string OrderId = "e0000101-0001-4000-8000-000000000001";
    public const string FulfillmentId = "e0000104-0001-4000-8000-000000000001";
    public const string FulfillmentLineId = "e0000105-0001-4000-8000-000000000001";
    public const string SlotReservationId = "e0000107-0001-4000-8000-000000000001";
    public const string PickupOrderId = "e0000108-0001-4000-8000-000000000001";
    public const string InventoryReservationId = "e0000109-0001-4000-8000-000000000001";
    public const string InventoryReservationLineId = "e0000110-0001-4000-8000-000000000001";

    public const string UpSql = """
        DO $startable$
        DECLARE
            seed_tenant constant uuid := '55555555-0000-4000-8000-000000000001';
            seed_order constant uuid := 'e0000101-0001-4000-8000-000000000001';
            seed_outlet constant uuid := 'bbbbbbbb-0001-4000-8000-000000000001';
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM sales_orders
                WHERE id = seed_order AND tenant_id = seed_tenant
                  AND order_number = 'ECOMM-SEED-PENDING-001'
                  AND reporting_outlet_id = seed_outlet
            ) THEN
                RAISE EXCEPTION 'BLOCKED — ECOMM-SEED-PENDING-001 sales order missing for startable New repair';
            END IF;

            IF EXISTS (
                SELECT 1 FROM fulfillment_orders f
                WHERE f.id = 'e0000104-0001-4000-8000-000000000001'
                  AND (f.tenant_id <> seed_tenant OR f.sales_order_id <> seed_order)
            ) THEN
                RAISE EXCEPTION 'BLOCKED — EXISTING DEVELOPMENT SEED GRAPH CONFLICT: PENDING-001 fulfillment id';
            END IF;
        END $startable$;

        -- Future collection window + New-eligible sales projection.
        UPDATE sales_orders
        SET order_status = 'CONFIRMED',
            fulfillment_status = 'PENDING',
            requested_collection_at = now() + interval '1 day',
            requested_collection_end_at = now() + interval '1 day 30 minutes',
            collection_timezone_snapshot = 'Asia/Colombo',
            completed_at = NULL,
            cancelled_at = NULL,
            updated_at = now()
        WHERE id = 'e0000101-0001-4000-8000-000000000001'
          AND tenant_id = '55555555-0000-4000-8000-000000000001'
          AND order_number = 'ECOMM-SEED-PENDING-001'
          AND reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001';

        WITH seed_order AS (
            SELECT so.*
            FROM sales_orders so
            JOIN fulfillment_method_outlets fmo
              ON fmo.id = so.fulfillment_method_outlet_id
             AND fmo.tenant_id = so.tenant_id
             AND fmo.outlet_id = so.reporting_outlet_id
            WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
              AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
              AND so.order_number = 'ECOMM-SEED-PENDING-001'
              AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
        )
        INSERT INTO fulfillment_orders (
            id, tenant_id, sales_order_id, fulfillment_number,
            fulfillment_method_outlet_id, source_inventory_location_id,
            fulfillment_status, requested_fulfillment_date, scheduled_at,
            assigned_to_tenant_user_id, row_version, created_at, updated_at)
        SELECT
            'e0000104-0001-4000-8000-000000000001', so.tenant_id, so.id,
            'FUL-ECOMM-SEED-PENDING-001', so.fulfillment_method_outlet_id,
            location.id, 'PENDING', so.requested_collection_at::date,
            so.requested_collection_at, NULL, 1, now(), now()
        FROM seed_order so
        LEFT JOIN inventory_locations location
          ON location.tenant_id = so.tenant_id
         AND location.outlet_id = so.reporting_outlet_id
         AND location.location_code = 'MAIN'
         AND location.status = 'ACTIVE'
        WHERE NOT EXISTS (
            SELECT 1 FROM fulfillment_orders existing
            WHERE existing.tenant_id = so.tenant_id
              AND existing.sales_order_id = so.id);

        -- If a prior runtime advanced this FO, reset only PENDING-001 back to startable.
        UPDATE fulfillment_orders f
        SET fulfillment_status = 'PENDING',
            assigned_to_tenant_user_id = NULL,
            row_version = GREATEST(f.row_version, 1),
            updated_at = now()
        FROM sales_orders so
        WHERE f.tenant_id = so.tenant_id
          AND f.sales_order_id = so.id
          AND so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND f.fulfillment_status <> 'PENDING';

        WITH seed_order AS (
            SELECT so.* FROM sales_orders so
            WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
              AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
              AND so.order_number = 'ECOMM-SEED-PENDING-001'
        )
        INSERT INTO fulfillment_order_lines (
            id, tenant_id, fulfillment_order_id, sales_order_line_id,
            requested_quantity, picked_quantity, packed_quantity,
            fulfilled_quantity, cancelled_quantity, line_status,
            created_at, updated_at)
        SELECT
            'e0000105-0001-4000-8000-000000000001', line.tenant_id,
            fulfillment.id, line.id, line.quantity, 0, 0, 0,
            line.cancelled_quantity, 'PENDING', now(), now()
        FROM seed_order so
        JOIN sales_order_lines line
          ON line.tenant_id = so.tenant_id AND line.sales_order_id = so.id
        JOIN LATERAL (
            SELECT f.id FROM fulfillment_orders f
            WHERE f.tenant_id = so.tenant_id AND f.sales_order_id = so.id
            ORDER BY f.created_at, f.id LIMIT 1
        ) fulfillment ON true
        WHERE NOT EXISTS (
            SELECT 1 FROM fulfillment_order_lines existing
            WHERE existing.tenant_id = line.tenant_id
              AND existing.fulfillment_order_id = fulfillment.id
              AND existing.sales_order_line_id = line.id);

        UPDATE fulfillment_order_lines fol
        SET picked_quantity = 0,
            packed_quantity = 0,
            fulfilled_quantity = 0,
            line_status = 'PENDING',
            updated_at = now()
        FROM fulfillment_orders f
        JOIN sales_orders so ON so.id = f.sales_order_id AND so.tenant_id = f.tenant_id
        WHERE fol.fulfillment_order_id = f.id
          AND fol.tenant_id = f.tenant_id
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND so.tenant_id = '55555555-0000-4000-8000-000000000001';

        WITH seed_order AS (
            SELECT so.* FROM sales_orders so
            WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
              AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
              AND so.order_number = 'ECOMM-SEED-PENDING-001'
        )
        INSERT INTO pickup_slots (
            id, tenant_id, fulfillment_method_outlet_id, slot_code,
            slot_date, window_start, window_end, capacity, reserved_count,
            slot_status, row_version, created_at, updated_at)
        SELECT
            'e0000106-0001-4000-8000-000000000001', so.tenant_id,
            so.fulfillment_method_outlet_id, 'ECOMM-SEED-RUNTIME',
            so.requested_collection_at::date,
            CASE
                WHEN so.requested_collection_end_at::time > so.requested_collection_at::time
                THEN so.requested_collection_at::time
                ELSE time '00:00:00'
            END,
            CASE
                WHEN so.requested_collection_end_at::time > so.requested_collection_at::time
                THEN so.requested_collection_end_at::time
                ELSE time '23:59:59'
            END,
            50, 0, 'OPEN', 1, now(), now()
        FROM seed_order so
        WHERE NOT EXISTS (
            SELECT 1 FROM pickup_slots ps
            WHERE ps.tenant_id = so.tenant_id
              AND ps.fulfillment_method_outlet_id = so.fulfillment_method_outlet_id
              AND ps.slot_code = 'ECOMM-SEED-RUNTIME');

        INSERT INTO pickup_slot_reservations (
            id, tenant_id, pickup_slot_id, sales_order_id, reserved_capacity,
            reservation_status, expires_at, confirmed_at, created_at, updated_at)
        SELECT
            'e0000107-0001-4000-8000-000000000001', so.tenant_id, ps.id, so.id, 1,
            'CONFIRMED', now() + interval '7 days', now(), now(), now()
        FROM sales_orders so
        JOIN pickup_slots ps
          ON ps.tenant_id = so.tenant_id
         AND ps.fulfillment_method_outlet_id = so.fulfillment_method_outlet_id
         AND ps.slot_code = 'ECOMM-SEED-RUNTIME'
        WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND NOT EXISTS (
              SELECT 1 FROM pickup_slot_reservations x
              WHERE x.tenant_id = so.tenant_id AND x.sales_order_id = so.id);

        UPDATE pickup_slot_reservations r
        SET reservation_status = 'CONFIRMED',
            expires_at = now() + interval '7 days',
            confirmed_at = COALESCE(r.confirmed_at, now()),
            updated_at = now()
        FROM sales_orders so
        WHERE r.tenant_id = so.tenant_id
          AND r.sales_order_id = so.id
          AND so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001';

        INSERT INTO pickup_orders (
            id, tenant_id, fulfillment_order_id, pickup_slot_reservation_id,
            pickup_number, pickup_contact_name, pickup_contact_phone,
            pickup_contact_email, pickup_contact_channel, pickup_status,
            created_at, updated_at)
        SELECT
            'e0000108-0001-4000-8000-000000000001', so.tenant_id, f.id, r.id,
            'PU-ECOMM-SEED-PENDING-001',
            COALESCE(NULLIF(so.customer_name_snapshot, ''), 'Development Customer'),
            so.customer_phone_snapshot, so.customer_email_snapshot,
            'EMAIL', 'PENDING', now(), now()
        FROM sales_orders so
        JOIN fulfillment_orders f
          ON f.tenant_id = so.tenant_id AND f.sales_order_id = so.id
        JOIN pickup_slot_reservations r
          ON r.tenant_id = so.tenant_id AND r.sales_order_id = so.id
         AND r.reservation_status = 'CONFIRMED'
        WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND NOT EXISTS (
              SELECT 1 FROM pickup_orders x
              WHERE x.tenant_id = so.tenant_id AND x.fulfillment_order_id = f.id);

        UPDATE pickup_orders po
        SET pickup_status = 'PENDING',
            updated_at = now()
        FROM fulfillment_orders f
        JOIN sales_orders so ON so.id = f.sales_order_id AND so.tenant_id = f.tenant_id
        WHERE po.fulfillment_order_id = f.id
          AND po.tenant_id = f.tenant_id
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND so.tenant_id = '55555555-0000-4000-8000-000000000001';

        INSERT INTO inventory_reservations (
            id, tenant_id, reservation_number, reservation_source,
            source_reference_id, source_reference_number, sales_channel_id,
            fulfillment_outlet_id, customer_id, reservation_status,
            reserved_at, expires_at, created_at, updated_at)
        SELECT
            'e0000109-0001-4000-8000-000000000001', so.tenant_id,
            'IR-ECOMM-SEED-PENDING-001', 'ORDER', so.id, so.order_number,
            so.sales_channel_id, so.reporting_outlet_id, so.customer_id,
            'CONFIRMED', now(), now() + interval '7 days', now(), now()
        FROM sales_orders so
        WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND NOT EXISTS (
              SELECT 1 FROM inventory_reservations x
              WHERE x.tenant_id = so.tenant_id
                AND x.source_reference_id = so.id);

        UPDATE inventory_reservations ir
        SET reservation_status = 'CONFIRMED',
            expires_at = now() + interval '7 days',
            updated_at = now()
        FROM sales_orders so
        WHERE ir.tenant_id = so.tenant_id
          AND ir.source_reference_id = so.id
          AND so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001';

        INSERT INTO inventory_reservation_lines (
            id, tenant_id, inventory_reservation_id, line_number,
            product_id, product_variant_id, requested_quantity,
            reserved_quantity, released_quantity, fulfilled_quantity,
            line_status, created_at, updated_at)
        SELECT
            'e0000110-0001-4000-8000-000000000001', line.tenant_id, ir.id,
            line.line_number, line.product_id, line.product_variant_id,
            line.quantity, line.quantity, 0, 0, 'RESERVED', now(), now()
        FROM sales_orders so
        JOIN sales_order_lines line
          ON line.tenant_id = so.tenant_id AND line.sales_order_id = so.id
        JOIN inventory_reservations ir
          ON ir.tenant_id = so.tenant_id AND ir.source_reference_id = so.id
        WHERE so.id = 'e0000101-0001-4000-8000-000000000001'
          AND so.order_number = 'ECOMM-SEED-PENDING-001'
          AND NOT EXISTS (
              SELECT 1 FROM inventory_reservation_lines x
              WHERE x.tenant_id = ir.tenant_id
                AND x.inventory_reservation_id = ir.id
                AND x.product_variant_id = line.product_variant_id);
        """;
}
