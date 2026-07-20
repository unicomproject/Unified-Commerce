using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStorefrontRequestedCollectionWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "collection_timezone_snapshot",
                table: "sales_orders",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_collection_at",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_collection_end_at",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "collection_timezone_snapshot",
                table: "checkout_sessions",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_collection_at",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_collection_end_at",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            // Preserve the selected collection window for active and historical checkout sessions
            // before retiring the capacity-booking slot reference. The new flow treats this as a
            // customer-requested time and does not reserve pickup slot capacity.
            migrationBuilder.Sql(
                """
                UPDATE checkout_sessions AS checkout
                SET requested_collection_at =
                        (slot.slot_date + slot.window_start) AT TIME ZONE outlet.timezone,
                    requested_collection_end_at =
                        (slot.slot_date + slot.window_end) AT TIME ZONE outlet.timezone,
                    collection_timezone_snapshot = outlet.timezone
                FROM pickup_slots AS slot
                INNER JOIN fulfillment_method_outlets AS method_outlet
                    ON method_outlet.id = slot.fulfillment_method_outlet_id
                   AND method_outlet.tenant_id = slot.tenant_id
                INNER JOIN outlets AS outlet
                    ON outlet.id = method_outlet.outlet_id
                   AND outlet.tenant_id = slot.tenant_id
                WHERE checkout.selected_pickup_slot_id = slot.id
                  AND checkout.tenant_id = slot.tenant_id
                  AND checkout.selected_outlet_id = outlet.id;
                """);

            migrationBuilder.DropColumn(
                name: "selected_pickup_slot_id",
                table: "checkout_sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "collection_timezone_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "requested_collection_at",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "requested_collection_end_at",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "collection_timezone_snapshot",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "requested_collection_at",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "requested_collection_end_at",
                table: "checkout_sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "selected_pickup_slot_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);
        }
    }
}
