using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_capacity",
                table: "pickup_slots",
                sql: "capacity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_reserved_count",
                table: "pickup_slots",
                sql: "reserved_count >= 0 AND reserved_count <= capacity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_row_version",
                table: "pickup_slots",
                sql: "row_version >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_slot_status",
                table: "pickup_slots",
                sql: "slot_status IN ('OPEN', 'FULL', 'CLOSED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_window_end",
                table: "pickup_slots",
                sql: "window_end > window_start");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slot_reservations_reserved_capacity",
                table: "pickup_slot_reservations",
                sql: "reserved_capacity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slot_reservations_session_or_order",
                table: "pickup_slot_reservations",
                sql: "checkout_session_id IS NOT NULL OR sales_order_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slot_reservations_status",
                table: "pickup_slot_reservations",
                sql: "reservation_status IN ('PENDING', 'CONFIRMED', 'RELEASED', 'EXPIRED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_orders_qr_version",
                table: "pickup_orders",
                sql: "pickup_qr_version IS NULL OR pickup_qr_version > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_orders_status",
                table: "pickup_orders",
                sql: "pickup_status IN ('PENDING', 'READY', 'VERIFIED', 'COLLECTED', 'CANCELLED', 'EXPIRED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_order_events_sequence_number",
                table: "pickup_order_events",
                sql: "sequence_number > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_orders_status",
                table: "fulfillment_orders",
                sql: "fulfillment_status IN ('PENDING', 'ALLOCATED', 'PICKING', 'PICKED', 'PACKED', 'READY', 'FULFILLED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_cancelled_quantity",
                table: "fulfillment_order_lines",
                sql: "cancelled_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_fulfilled_quantity",
                table: "fulfillment_order_lines",
                sql: "fulfilled_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_packed_quantity",
                table: "fulfillment_order_lines",
                sql: "packed_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_picked_quantity",
                table: "fulfillment_order_lines",
                sql: "picked_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_requested_quantity",
                table: "fulfillment_order_lines",
                sql: "requested_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_status",
                table: "fulfillment_order_lines",
                sql: "line_status IN ('PENDING', 'PICKING', 'PICKED', 'PACKED', 'FULFILLED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_events_sequence_number",
                table: "fulfillment_order_events",
                sql: "sequence_number > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_methods_method_type",
                table: "fulfillment_methods",
                sql: "method_type IN ('IMMEDIATE', 'PICKUP')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_methods_status",
                table: "fulfillment_methods",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_method_outlets_pickup_window_minutes",
                table: "fulfillment_method_outlets",
                sql: "pickup_window_minutes IS NULL OR pickup_window_minutes > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_method_outlets_preparation_lead_minutes",
                table: "fulfillment_method_outlets",
                sql: "preparation_lead_minutes IS NULL OR preparation_lead_minutes >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_method_outlets_status",
                table: "fulfillment_method_outlets",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_capacity",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_reserved_count",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_row_version",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_slot_status",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_window_end",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slot_reservations_reserved_capacity",
                table: "pickup_slot_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slot_reservations_session_or_order",
                table: "pickup_slot_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slot_reservations_status",
                table: "pickup_slot_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_orders_qr_version",
                table: "pickup_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_orders_status",
                table: "pickup_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_order_events_sequence_number",
                table: "pickup_order_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_orders_status",
                table: "fulfillment_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_cancelled_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_fulfilled_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_packed_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_picked_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_requested_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_status",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_events_sequence_number",
                table: "fulfillment_order_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_methods_method_type",
                table: "fulfillment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_methods_status",
                table: "fulfillment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_method_outlets_pickup_window_minutes",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_method_outlets_preparation_lead_minutes",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_method_outlets_status",
                table: "fulfillment_method_outlets");
        }
    }
}
