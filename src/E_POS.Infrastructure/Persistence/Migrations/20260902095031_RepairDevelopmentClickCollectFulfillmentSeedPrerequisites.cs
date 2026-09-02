using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairDevelopmentClickCollectFulfillmentSeedPrerequisites : Migration
    {
        // Immutable payload owned by this migration. Do not move this SQL back to
        // the historical shared Development seed helper.
        public const string RepairSql = """
            DO $repair$
            DECLARE
                seed_tenant constant uuid := '55555555-0000-4000-8000-000000000001';
                seed_order constant uuid := 'e0000101-0003-4000-8000-000000000001';
                seed_outlet constant uuid := 'bbbbbbbb-0001-4000-8000-000000000001';
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sales_orders
                    WHERE id = seed_order AND tenant_id = seed_tenant
                      AND order_number = 'ECOMM-SEED-ACCEPTED-001'
                      AND (reporting_outlet_id <> seed_outlet OR fulfillment_method_outlet_id IS NULL)
                ) THEN
                    RAISE EXCEPTION 'BLOCKED — EXISTING DEVELOPMENT SEED GRAPH CONFLICT: sales order outlet/method ownership';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM sales_orders so
                    JOIN fulfillment_method_outlets fmo
                      ON fmo.id = so.fulfillment_method_outlet_id
                     AND fmo.tenant_id = so.tenant_id
                    WHERE so.id = seed_order AND so.tenant_id = seed_tenant
                      AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                      AND fmo.outlet_id <> seed_outlet
                ) THEN
                    RAISE EXCEPTION 'BLOCKED — EXISTING DEVELOPMENT SEED GRAPH CONFLICT: pickup method outlet';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM fulfillment_orders f
                    WHERE f.tenant_id = seed_tenant AND f.sales_order_id = seed_order
                      AND f.fulfillment_method_outlet_id <> (
                          SELECT so.fulfillment_method_outlet_id FROM sales_orders so
                          WHERE so.id = seed_order AND so.tenant_id = seed_tenant
                      )
                ) THEN
                    RAISE EXCEPTION 'BLOCKED — EXISTING DEVELOPMENT SEED GRAPH CONFLICT: fulfillment outlet';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM fulfillment_orders f
                    WHERE f.id = 'e0000104-0003-4000-8000-000000000001'
                      AND (f.tenant_id <> seed_tenant OR f.sales_order_id <> seed_order)
                ) OR EXISTS (
                    SELECT 1 FROM pickup_slots ps
                    WHERE ps.id = 'e0000106-0001-4000-8000-000000000001'
                      AND ps.tenant_id <> seed_tenant
                ) OR EXISTS (
                    SELECT 1 FROM inventory_reservations ir
                    WHERE ir.id = 'e0000109-0003-4000-8000-000000000001'
                      AND (ir.tenant_id <> seed_tenant OR ir.source_reference_id <> seed_order)
                ) THEN
                    RAISE EXCEPTION 'BLOCKED — EXISTING DEVELOPMENT SEED GRAPH CONFLICT: deterministic identifier collision';
                END IF;
            END $repair$;

            WITH seed_order AS (
                SELECT so.*
                FROM sales_orders so
                JOIN fulfillment_method_outlets fmo
                  ON fmo.id = so.fulfillment_method_outlet_id
                 AND fmo.tenant_id = so.tenant_id
                 AND fmo.outlet_id = so.reporting_outlet_id
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
            )
            INSERT INTO fulfillment_orders (
                id, tenant_id, sales_order_id, fulfillment_number,
                fulfillment_method_outlet_id, source_inventory_location_id,
                fulfillment_status, requested_fulfillment_date, scheduled_at,
                row_version, created_at, updated_at)
            SELECT
                'e0000104-0003-4000-8000-000000000001', so.tenant_id, so.id,
                'FUL-ECOMM-SEED-ACCEPTED-001', so.fulfillment_method_outlet_id,
                location.id, 'PENDING', so.requested_collection_at::date,
                so.requested_collection_at, 1, now(), now()
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

            WITH seed_order AS (
                SELECT so.* FROM sales_orders so
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
            )
            INSERT INTO fulfillment_order_lines (
                id, tenant_id, fulfillment_order_id, sales_order_line_id,
                requested_quantity, picked_quantity, packed_quantity,
                fulfilled_quantity, cancelled_quantity, line_status,
                created_at, updated_at)
            SELECT
                'e0000105-0003-4000-8000-000000000001', line.tenant_id,
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

            WITH seed_order AS (
                SELECT so.* FROM sales_orders so
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
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
                    ELSE time '23:59:59.999999'
                END,
                100, 1, 'OPEN', 1, now(), now()
            FROM seed_order so
            WHERE NOT EXISTS (
                SELECT 1 FROM pickup_slots existing
                WHERE existing.tenant_id = so.tenant_id
                  AND existing.fulfillment_method_outlet_id = so.fulfillment_method_outlet_id
                  AND existing.slot_code = 'ECOMM-SEED-RUNTIME');

            WITH seed_order AS (
                SELECT so.* FROM sales_orders so
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
            )
            INSERT INTO pickup_slot_reservations (
                id, tenant_id, pickup_slot_id, sales_order_id, reserved_capacity,
                reservation_status, expires_at, confirmed_at, created_at, updated_at)
            SELECT
                'e0000107-0003-4000-8000-000000000001', so.tenant_id,
                slot.id, so.id, 1, 'CONFIRMED', now() + interval '7 days',
                now(), now(), now()
            FROM seed_order so
            JOIN pickup_slots slot
              ON slot.tenant_id = so.tenant_id
             AND slot.fulfillment_method_outlet_id = so.fulfillment_method_outlet_id
             AND slot.slot_code = 'ECOMM-SEED-RUNTIME'
            WHERE NOT EXISTS (
                SELECT 1 FROM pickup_slot_reservations existing
                WHERE existing.tenant_id = so.tenant_id
                  AND existing.sales_order_id = so.id);

            WITH seed_order AS (
                SELECT so.* FROM sales_orders so
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
            )
            INSERT INTO pickup_orders (
                id, tenant_id, fulfillment_order_id, pickup_slot_reservation_id,
                pickup_number, pickup_contact_name, pickup_contact_phone,
                pickup_contact_email, pickup_contact_channel, pickup_status,
                created_at, updated_at)
            SELECT
                'e0000108-0003-4000-8000-000000000001', so.tenant_id,
                fulfillment.id, reservation.id, 'PU-ECOMM-SEED-ACCEPTED-001',
                COALESCE(NULLIF(so.customer_name_snapshot, ''), 'Development Customer'),
                so.customer_phone_snapshot, so.customer_email_snapshot,
                'EMAIL', 'PENDING', now(), now()
            FROM seed_order so
            JOIN LATERAL (
                SELECT f.id FROM fulfillment_orders f
                WHERE f.tenant_id = so.tenant_id AND f.sales_order_id = so.id
                ORDER BY f.created_at, f.id LIMIT 1
            ) fulfillment ON true
            JOIN pickup_slot_reservations reservation
              ON reservation.tenant_id = so.tenant_id
             AND reservation.sales_order_id = so.id
             AND reservation.reservation_status = 'CONFIRMED'
            WHERE NOT EXISTS (
                SELECT 1 FROM pickup_orders existing
                WHERE existing.tenant_id = so.tenant_id
                  AND existing.fulfillment_order_id = fulfillment.id);

            WITH seed_order AS (
                SELECT so.* FROM sales_orders so
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
            )
            INSERT INTO inventory_reservations (
                id, tenant_id, reservation_number, reservation_source,
                source_reference_id, source_reference_number, sales_channel_id,
                fulfillment_outlet_id, customer_id, reservation_status,
                reserved_at, expires_at, created_at, updated_at)
            SELECT
                'e0000109-0003-4000-8000-000000000001', so.tenant_id,
                'RES-ECOMM-SEED-ACCEPTED-001', 'ORDER', so.id, so.order_number,
                so.sales_channel_id, so.reporting_outlet_id, so.customer_id,
                'CONFIRMED', now(), now() + interval '7 days', now(), now()
            FROM seed_order so
            WHERE NOT EXISTS (
                SELECT 1 FROM inventory_reservations existing
                WHERE existing.tenant_id = so.tenant_id
                  AND existing.source_reference_id = so.id
                  AND existing.fulfillment_outlet_id = so.reporting_outlet_id
                  AND existing.reservation_status = 'CONFIRMED');

            WITH seed_order AS (
                SELECT so.* FROM sales_orders so
                WHERE so.id = 'e0000101-0003-4000-8000-000000000001'
                  AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND so.order_number = 'ECOMM-SEED-ACCEPTED-001'
                  AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
            )
            INSERT INTO inventory_reservation_lines (
                id, tenant_id, inventory_reservation_id, line_number,
                product_id, product_variant_id, requested_quantity,
                reserved_quantity, released_quantity, fulfilled_quantity,
                line_status, created_at, updated_at)
            SELECT
                'e0000110-0003-4000-8000-000000000001', line.tenant_id,
                reservation.id, line.line_number, line.product_id,
                line.product_variant_id, line.quantity, line.quantity,
                0, 0, 'RESERVED', now(), now()
            FROM seed_order so
            JOIN sales_order_lines line
              ON line.tenant_id = so.tenant_id AND line.sales_order_id = so.id
            JOIN LATERAL (
                SELECT ir.id FROM inventory_reservations ir
                WHERE ir.tenant_id = so.tenant_id
                  AND ir.source_reference_id = so.id
                  AND ir.fulfillment_outlet_id = so.reporting_outlet_id
                  AND ir.reservation_status = 'CONFIRMED'
                ORDER BY ir.created_at, ir.id LIMIT 1
            ) reservation ON true
            WHERE NOT EXISTS (
                SELECT 1 FROM inventory_reservation_lines existing
                WHERE existing.tenant_id = line.tenant_id
                  AND existing.inventory_reservation_id = reservation.id
                  AND existing.line_number = line.line_number);
            """;

        // The original repair covered only ACCEPTED-001, which left otherwise
        // equivalent Development orders without the operational graph required
        // by OO-03/OO-04. Keep the same authoritative relationships for every
        // ACCEPTED seed order; no production order is matched by this query.
        public const string AdditionalAcceptedOrdersSql = """
            WITH targets AS (
                SELECT * FROM (VALUES
                    ('e0000101-0004-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-002',
                     'e0000104-0004-4000-8000-000000000001'::uuid,
                     'e0000105-0004-4000-8000-000000000001'::uuid,
                     'e0000107-0004-4000-8000-000000000001'::uuid,
                     'e0000108-0004-4000-8000-000000000001'::uuid,
                     'e0000109-0004-4000-8000-000000000001'::uuid,
                     'e0000110-0004-4000-8000-000000000001'::uuid),
                    ('e0000101-0012-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-003',
                     'e0000104-0012-4000-8000-000000000001'::uuid,
                     'e0000105-0012-4000-8000-000000000001'::uuid,
                     'e0000107-0012-4000-8000-000000000001'::uuid,
                     'e0000108-0012-4000-8000-000000000001'::uuid,
                     'e0000109-0012-4000-8000-000000000001'::uuid,
                     'e0000110-0012-4000-8000-000000000001'::uuid)
                ) v(order_id, order_number, fulfillment_id, fulfillment_line_id,
                    slot_reservation_id, pickup_order_id, inventory_reservation_id,
                    inventory_reservation_line_id)
            ), valid_orders AS (
                SELECT t.*, so.tenant_id, so.fulfillment_method_outlet_id,
                       so.reporting_outlet_id, so.requested_collection_at,
                       so.sales_channel_id, so.customer_id
                FROM targets t
                JOIN sales_orders so ON so.id = t.order_id
                 AND so.tenant_id = '55555555-0000-4000-8000-000000000001'
                 AND so.order_number = t.order_number
                 AND so.reporting_outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001'
                JOIN fulfillment_method_outlets fmo
                  ON fmo.id = so.fulfillment_method_outlet_id
                 AND fmo.tenant_id = so.tenant_id
                 AND fmo.outlet_id = so.reporting_outlet_id
            )
            INSERT INTO fulfillment_orders (
                id, tenant_id, sales_order_id, fulfillment_number,
                fulfillment_method_outlet_id, source_inventory_location_id,
                fulfillment_status, requested_fulfillment_date, scheduled_at,
                row_version, created_at, updated_at)
            SELECT v.fulfillment_id, v.tenant_id, v.order_id,
                   'FUL-' || v.order_number, v.fulfillment_method_outlet_id,
                   location.id, 'PENDING', v.requested_collection_at::date,
                   v.requested_collection_at, 1, now(), now()
            FROM valid_orders v
            LEFT JOIN inventory_locations location
              ON location.tenant_id = v.tenant_id
             AND location.outlet_id = v.reporting_outlet_id
             AND location.location_code = 'MAIN' AND location.status = 'ACTIVE'
            WHERE NOT EXISTS (SELECT 1 FROM fulfillment_orders f
                WHERE f.tenant_id = v.tenant_id AND f.sales_order_id = v.order_id);

            WITH targets AS (
                SELECT * FROM (VALUES
                    ('e0000101-0004-4000-8000-000000000001'::uuid, 'e0000104-0004-4000-8000-000000000001'::uuid, 'e0000105-0004-4000-8000-000000000001'::uuid),
                    ('e0000101-0012-4000-8000-000000000001'::uuid, 'e0000104-0012-4000-8000-000000000001'::uuid, 'e0000105-0012-4000-8000-000000000001'::uuid)
                ) v(order_id, fulfillment_id, line_id)
            )
            INSERT INTO fulfillment_order_lines (
                id, tenant_id, fulfillment_order_id, sales_order_line_id,
                requested_quantity, picked_quantity, packed_quantity,
                fulfilled_quantity, cancelled_quantity, line_status, created_at, updated_at)
            SELECT t.line_id, l.tenant_id, f.id, l.id, l.quantity, 0, 0, 0,
                   l.cancelled_quantity, 'PENDING', now(), now()
            FROM targets t
            JOIN fulfillment_orders f ON f.id = t.fulfillment_id AND f.sales_order_id = t.order_id
            JOIN sales_order_lines l ON l.tenant_id = f.tenant_id AND l.sales_order_id = t.order_id
            WHERE NOT EXISTS (SELECT 1 FROM fulfillment_order_lines x
                WHERE x.tenant_id = l.tenant_id AND x.fulfillment_order_id = f.id
                  AND x.sales_order_line_id = l.id);

            WITH targets AS (
                SELECT * FROM (VALUES
                    ('e0000101-0004-4000-8000-000000000001'::uuid, 'e0000107-0004-4000-8000-000000000001'::uuid),
                    ('e0000101-0012-4000-8000-000000000001'::uuid, 'e0000107-0012-4000-8000-000000000001'::uuid)
                ) v(order_id, reservation_id)
            )
            INSERT INTO pickup_slot_reservations (
                id, tenant_id, pickup_slot_id, sales_order_id, reserved_capacity,
                reservation_status, expires_at, confirmed_at, created_at, updated_at)
            SELECT t.reservation_id, so.tenant_id, ps.id, so.id, 1, 'CONFIRMED',
                   now() + interval '7 days', now(), now(), now()
            FROM targets t JOIN sales_orders so ON so.id = t.order_id
            JOIN pickup_slots ps ON ps.tenant_id = so.tenant_id
             AND ps.fulfillment_method_outlet_id = so.fulfillment_method_outlet_id
             AND ps.slot_code = 'ECOMM-SEED-RUNTIME'
            WHERE NOT EXISTS (SELECT 1 FROM pickup_slot_reservations x
                WHERE x.tenant_id = so.tenant_id AND x.sales_order_id = so.id);

            WITH targets AS (
                SELECT * FROM (VALUES
                    ('e0000101-0004-4000-8000-000000000001'::uuid, 'e0000104-0004-4000-8000-000000000001'::uuid, 'e0000108-0004-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-002'),
                    ('e0000101-0012-4000-8000-000000000001'::uuid, 'e0000104-0012-4000-8000-000000000001'::uuid, 'e0000108-0012-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-003')
                ) v(order_id, fulfillment_id, pickup_id, order_number)
            )
            INSERT INTO pickup_orders (
                id, tenant_id, fulfillment_order_id, pickup_slot_reservation_id,
                pickup_number, pickup_contact_name, pickup_contact_phone,
                pickup_contact_email, pickup_contact_channel, pickup_status,
                created_at, updated_at)
            SELECT t.pickup_id, so.tenant_id, t.fulfillment_id, r.id,
                   'PU-' || t.order_number,
                   COALESCE(NULLIF(so.customer_name_snapshot, ''), 'Development Customer'),
                   so.customer_phone_snapshot, so.customer_email_snapshot,
                   'EMAIL', 'PENDING', now(), now()
            FROM targets t JOIN sales_orders so ON so.id = t.order_id
            JOIN pickup_slot_reservations r ON r.tenant_id = so.tenant_id
             AND r.sales_order_id = so.id AND r.reservation_status = 'CONFIRMED'
            WHERE NOT EXISTS (SELECT 1 FROM pickup_orders x
                WHERE x.tenant_id = so.tenant_id AND x.fulfillment_order_id = t.fulfillment_id);

            WITH targets AS (
                SELECT * FROM (VALUES
                    ('e0000101-0004-4000-8000-000000000001'::uuid, 'e0000109-0004-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-002'),
                    ('e0000101-0012-4000-8000-000000000001'::uuid, 'e0000109-0012-4000-8000-000000000001'::uuid, 'ECOMM-SEED-ACCEPTED-003')
                ) v(order_id, reservation_id, order_number)
            )
            INSERT INTO inventory_reservations (
                id, tenant_id, reservation_number, reservation_source,
                source_reference_id, source_reference_number, sales_channel_id,
                fulfillment_outlet_id, customer_id, reservation_status,
                reserved_at, expires_at, created_at, updated_at)
            SELECT t.reservation_id, so.tenant_id, 'RES-' || t.order_number,
                   'ORDER', so.id, so.order_number, so.sales_channel_id,
                   so.reporting_outlet_id, so.customer_id, 'CONFIRMED',
                   now(), now() + interval '7 days', now(), now()
            FROM targets t JOIN sales_orders so ON so.id = t.order_id
            WHERE NOT EXISTS (SELECT 1 FROM inventory_reservations x
                WHERE x.tenant_id = so.tenant_id AND x.source_reference_id = so.id
                  AND x.fulfillment_outlet_id = so.reporting_outlet_id
                  AND x.reservation_status = 'CONFIRMED');

            WITH targets AS (
                SELECT * FROM (VALUES
                    ('e0000101-0004-4000-8000-000000000001'::uuid, 'e0000109-0004-4000-8000-000000000001'::uuid, 'e0000110-0004-4000-8000-000000000001'::uuid),
                    ('e0000101-0012-4000-8000-000000000001'::uuid, 'e0000109-0012-4000-8000-000000000001'::uuid, 'e0000110-0012-4000-8000-000000000001'::uuid)
                ) v(order_id, reservation_id, line_id)
            )
            INSERT INTO inventory_reservation_lines (
                id, tenant_id, inventory_reservation_id, line_number,
                product_id, product_variant_id, requested_quantity,
                reserved_quantity, released_quantity, fulfilled_quantity,
                line_status, created_at, updated_at)
            SELECT t.line_id, l.tenant_id, t.reservation_id, l.line_number,
                   l.product_id, l.product_variant_id, l.quantity, l.quantity,
                   0, 0, 'RESERVED', now(), now()
            FROM targets t JOIN sales_order_lines l ON l.sales_order_id = t.order_id
            WHERE NOT EXISTS (SELECT 1 FROM inventory_reservation_lines x
                WHERE x.tenant_id = l.tenant_id
                  AND x.inventory_reservation_id = t.reservation_id
                  AND x.line_number = l.line_number);
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RepairSql);
            migrationBuilder.Sql(AdditionalAcceptedOrdersSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally non-destructive. The repaired graph may have entered
            // an operational lifecycle after migration application.
        }
    }
}
