using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingEcommerceFulfillmentGraph : Migration
    {
        // One-time, idempotent repair for the checkout-confirmation gap where e-commerce
        // Click & Collect orders were confirmed (SalesOrder + SalesOrderLines created) without
        // the POS fulfilment graph (FulfillmentOrder/FulfillmentOrderLines/PickupOrder/pickup
        // slot reservation). Going forward, StorefrontCheckoutConfirmationRepository creates
        // this graph live; this migration only backfills orders that already exist.
        //
        // An order is only repaired when we can prove, from data it already has, exactly what
        // the checkout-confirmation code would have written: it must be a non-cancelled
        // CLICK_AND_COLLECT order with its originating checkout session and a CONFIRMED
        // inventory reservation still attached, plus a resolvable pickup method/outlet and
        // collection window. Every insert is guarded by NOT EXISTS and keyed by a deterministic
        // id derived from the source row, so re-running this migration is a no-op and it can
        // never create a second fulfilment graph for the same order.
        public const string RepairFulfillmentGraphSql = """
            WITH eligible_orders AS (
                SELECT so.id, so.tenant_id, so.order_number, so.fulfillment_method_outlet_id,
                       so.requested_collection_at, so.requested_collection_end_at,
                       so.collection_timezone_snapshot,
                       COALESCE(NULLIF(so.customer_name_snapshot, ''), 'Customer') AS pickup_contact_name,
                       so.customer_phone_snapshot, so.customer_email_snapshot,
                       ir.id AS inventory_reservation_id
                FROM sales_orders so
                JOIN checkout_sessions cs ON cs.converted_order_id = so.id
                JOIN inventory_reservations ir
                  ON ir.id = cs.inventory_reservation_id
                 AND ir.reservation_status = 'CONFIRMED'
                WHERE so.order_type = 'CLICK_AND_COLLECT'
                  AND so.order_status <> 'CANCELLED'
                  AND so.fulfillment_method_outlet_id IS NOT NULL
                  AND so.requested_collection_at IS NOT NULL
                  AND so.requested_collection_end_at IS NOT NULL
                  AND so.collection_timezone_snapshot IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM fulfillment_orders fo
                      WHERE fo.tenant_id = so.tenant_id AND fo.sales_order_id = so.id)
            )
            INSERT INTO fulfillment_orders (
                id, tenant_id, sales_order_id, fulfillment_number,
                fulfillment_method_outlet_id, source_inventory_location_id,
                fulfillment_status, requested_fulfillment_date, scheduled_at,
                row_version, created_at, updated_at)
            SELECT
                md5('FULFILLMENT_ORDER:' || eo.id::text)::uuid,
                eo.tenant_id, eo.id, 'FUL-' || eo.order_number,
                eo.fulfillment_method_outlet_id, resolved_location.location_id,
                'PENDING',
                (eo.requested_collection_at AT TIME ZONE eo.collection_timezone_snapshot)::date,
                eo.requested_collection_at, 1, now(), now()
            FROM eligible_orders eo
            LEFT JOIN LATERAL (
                SELECT MIN(loc.location_id::text)::uuid AS location_id
                FROM (
                    SELECT DISTINCT bal.inventory_location_id AS location_id
                    FROM inventory_reservation_lines rl
                    JOIN inventory_reservation_allocations ra
                      ON ra.inventory_reservation_line_id = rl.id
                    JOIN inventory_balances bal ON bal.id = ra.inventory_balance_id
                    WHERE rl.inventory_reservation_id = eo.inventory_reservation_id
                ) loc
                HAVING count(*) = 1
            ) resolved_location ON true
            WHERE NOT EXISTS (
                SELECT 1 FROM fulfillment_orders existing
                WHERE existing.tenant_id = eo.tenant_id AND existing.sales_order_id = eo.id);

            INSERT INTO fulfillment_order_lines (
                id, tenant_id, fulfillment_order_id, sales_order_line_id,
                requested_quantity, picked_quantity, packed_quantity,
                fulfilled_quantity, cancelled_quantity, line_status, created_at, updated_at)
            SELECT
                md5('FULFILLMENT_ORDER_LINE:' || l.id::text)::uuid,
                l.tenant_id, fo.id, l.id, l.quantity, 0, 0, 0,
                l.cancelled_quantity, 'PENDING', now(), now()
            FROM fulfillment_orders fo
            JOIN sales_order_lines l
              ON l.tenant_id = fo.tenant_id AND l.sales_order_id = fo.sales_order_id
            WHERE NOT EXISTS (
                SELECT 1 FROM fulfillment_order_lines existing
                WHERE existing.tenant_id = l.tenant_id
                  AND existing.fulfillment_order_id = fo.id
                  AND existing.sales_order_line_id = l.id);

            WITH eligible_orders AS (
                SELECT so.id, so.tenant_id, so.order_number, so.fulfillment_method_outlet_id,
                       so.requested_collection_at, so.requested_collection_end_at,
                       so.collection_timezone_snapshot
                FROM sales_orders so
                JOIN fulfillment_orders fo ON fo.tenant_id = so.tenant_id AND fo.sales_order_id = so.id
            )
            INSERT INTO pickup_slots (
                id, tenant_id, fulfillment_method_outlet_id, slot_code,
                slot_date, window_start, window_end, capacity, reserved_count,
                slot_status, row_version, created_at, updated_at)
            SELECT
                md5('PICKUP_SLOT:' || eo.id::text)::uuid,
                eo.tenant_id, eo.fulfillment_method_outlet_id, 'ECOMM-' || eo.order_number,
                (eo.requested_collection_at AT TIME ZONE eo.collection_timezone_snapshot)::date,
                (eo.requested_collection_at AT TIME ZONE eo.collection_timezone_snapshot)::time,
                (eo.requested_collection_end_at AT TIME ZONE eo.collection_timezone_snapshot)::time,
                1, 1, 'FULL', 1, now(), now()
            FROM eligible_orders eo
            WHERE (eo.requested_collection_end_at AT TIME ZONE eo.collection_timezone_snapshot)::time
                > (eo.requested_collection_at AT TIME ZONE eo.collection_timezone_snapshot)::time
              AND NOT EXISTS (
                  SELECT 1 FROM pickup_slots existing
                  WHERE existing.tenant_id = eo.tenant_id
                    AND existing.fulfillment_method_outlet_id = eo.fulfillment_method_outlet_id
                    AND existing.slot_code = 'ECOMM-' || eo.order_number);

            INSERT INTO pickup_slot_reservations (
                id, tenant_id, pickup_slot_id, sales_order_id, reserved_capacity,
                reservation_status, expires_at, confirmed_at, created_at, updated_at)
            SELECT
                md5('PICKUP_SLOT_RESERVATION:' || so.id::text)::uuid,
                so.tenant_id, slot.id, so.id, 1, 'CONFIRMED', NULL, now(), now(), now()
            FROM sales_orders so
            JOIN fulfillment_orders fo ON fo.tenant_id = so.tenant_id AND fo.sales_order_id = so.id
            JOIN pickup_slots slot
              ON slot.tenant_id = so.tenant_id
             AND slot.fulfillment_method_outlet_id = so.fulfillment_method_outlet_id
             AND slot.slot_code = 'ECOMM-' || so.order_number
            WHERE NOT EXISTS (
                SELECT 1 FROM pickup_slot_reservations existing
                WHERE existing.tenant_id = so.tenant_id AND existing.sales_order_id = so.id);

            INSERT INTO pickup_orders (
                id, tenant_id, fulfillment_order_id, pickup_slot_reservation_id,
                pickup_number, pickup_contact_name, pickup_contact_phone,
                pickup_contact_email, pickup_contact_channel, pickup_status,
                created_at, updated_at)
            SELECT
                md5('PICKUP_ORDER:' || so.id::text)::uuid,
                so.tenant_id, fo.id, res.id, 'PU-' || so.order_number,
                COALESCE(NULLIF(so.customer_name_snapshot, ''), 'Customer'),
                so.customer_phone_snapshot, so.customer_email_snapshot,
                CASE
                    WHEN COALESCE(so.customer_email_snapshot, '') <> '' THEN 'EMAIL'
                    WHEN COALESCE(so.customer_phone_snapshot, '') <> '' THEN 'PHONE'
                    ELSE NULL
                END,
                'PENDING', now(), now()
            FROM sales_orders so
            JOIN fulfillment_orders fo ON fo.tenant_id = so.tenant_id AND fo.sales_order_id = so.id
            JOIN pickup_slot_reservations res
              ON res.tenant_id = so.tenant_id AND res.sales_order_id = so.id
             AND res.reservation_status = 'CONFIRMED'
            WHERE NOT EXISTS (
                SELECT 1 FROM pickup_orders existing
                WHERE existing.tenant_id = so.tenant_id AND existing.fulfillment_order_id = fo.id);
            """;

        // Companion root-cause repair 1: a CHECKOUT-sourced inventory reservation is
        // created before the SalesOrder exists and stores the checkout session id as its
        // source_reference_id. Checkout confirmation never re-pointed it at the resulting
        // order, so POS "start fulfillment" (which looks up the reservation by
        // source_reference_id = sales_order_id) can never find it even once the fulfilment
        // graph above exists. This re-points every confirmed e-commerce reservation still
        // referencing its checkout session at the order it belongs to; already-correct
        // rows are left untouched, so this is safe to run repeatedly.
        public const string RepointReservationSourceReferenceSql = """
            UPDATE inventory_reservations ir
            SET source_reference_id = so.id,
                source_reference_number = so.order_number,
                updated_at = now()
            FROM checkout_sessions cs
            JOIN sales_orders so ON so.id = cs.converted_order_id
            WHERE ir.id = cs.inventory_reservation_id
              AND ir.reservation_status = 'CONFIRMED'
              AND ir.reservation_source = 'CHECKOUT'
              AND so.order_type = 'CLICK_AND_COLLECT'
              AND ir.source_reference_id IS DISTINCT FROM so.id;
            """;

        // Companion root-cause repair 2: expires_at is a hold expiry for an in-flight
        // checkout and was never cleared on confirmation, so POS "start fulfillment"
        // (which also requires the reservation be unexpired) starts failing again a few
        // minutes after the order was placed even once the reservation correctly points
        // at the order. Runs after the repoint above so it also covers rows it just fixed,
        // and separately covers any already order-linked reservation (e.g. from an earlier,
        // narrower repair) that still carries a stale expiry. Only ever clears an already
        // meaningless field on a permanently confirmed reservation, so it is safe to run
        // repeatedly and cannot revive an order that should stay unfulfillable.
        public const string ClearStaleReservationExpirySql = """
            UPDATE inventory_reservations ir
            SET expires_at = NULL,
                updated_at = now()
            FROM sales_orders so
            WHERE ir.source_reference_id = so.id
              AND so.order_type = 'CLICK_AND_COLLECT'
              AND ir.reservation_status = 'CONFIRMED'
              AND ir.expires_at IS NOT NULL;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RepairFulfillmentGraphSql);
            migrationBuilder.Sql(RepointReservationSourceReferenceSql);
            migrationBuilder.Sql(ClearStaleReservationExpirySql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally non-destructive. The repaired graph may have entered an
            // operational lifecycle (picking/packing/collection) after this migration ran.
        }
    }
}
