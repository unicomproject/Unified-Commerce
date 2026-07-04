using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignDiscountInventoryStockErdSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_types_tenant_id_tenants",
                table: "discount_types");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_product_batch_id_product_batches",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_destination_inventory_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_source_inventory_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_lines_stocktake_session_id_product_id_product_variant_id_product_batch_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_line_serials_stocktake_line_id_serial_number_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_transfers_source_inventory_location_id_destination_in~",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_status_history_stock_transfer_id_sequence_number",
                table: "stock_transfer_status_history");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_lines_stock_transfer_id_line_number",
                table: "stock_transfer_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_transfer_lines_requested_quantity",
                table: "stock_transfer_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_movement_quantity",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_serials_stock_movement_id_serial_number_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_references_stock_movement_id_reference_type_reference_id",
                table: "stock_movement_references");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movement_cost_allocations_allocated_cost_amount",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropIndex(
                name: "uq_stock_adjustment_lines_stock_adjustment_id_line_number",
                table: "stock_adjustment_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_adjustment_lines_adjustment_quantity",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "uq_serial_numbers_tenant_id_serial_number",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "uq_product_inventory_settings_product_id_product_variant_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "uq_product_batches_tenant_id_product_id_product_variant_id_batch_number",
                table: "product_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_batches_expiry_date_manufactured_date",
                table: "product_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservations_reservation_status",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservation_lines_inventory_reservation_id_line_number",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_requested_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_allocated_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reorder_rules_inventory_location_id_product_id_product_variant_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_reorder_point_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "uq_inventory_locations_tenant_id_location_code",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_cost_layers_product_batch_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_quantity_remaining",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_unit_cost",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "uq_inventory_channel_allocations_inventory_location_id_product_id_product_variant_id_sales_channel_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_balances_inventory_location_id_product_id_product_variant_id_product_batch_id",
                table: "inventory_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_balances_on_hand_quantity",
                table: "inventory_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_days_before_expiry",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_discount_value",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "uq_expiry_discount_applications_product_batch_id_expiry_discount_rule_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "uq_discount_types_tenant_id_discount_type_code",
                table: "discount_types");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_targets_discount_policy_id_target_type_target_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_outlets_discount_policy_id_outlet_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_conditions_condition_sequence",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_channels_discount_policy_id_sales_channel_id",
                table: "discount_policy_channels");

            migrationBuilder.DropColumn(
                name: "status",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "stock_transfer_status_history");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "stock_movement_serials");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "stock_movement_references");

            migrationBuilder.DropColumn(
                name: "allocated_cost_amount",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropColumn(
                name: "description",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "name",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "name",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "product_batch_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "name",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "discount_value",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "name",
                table: "discount_types");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "discount_types");

            migrationBuilder.DropColumn(
                name: "name",
                table: "discount_policies");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "stocktake_line_serials",
                newName: "scanned_at");

            migrationBuilder.RenameColumn(
                name: "source_inventory_location_id",
                table: "stock_transfers",
                newName: "source_location_id");

            migrationBuilder.RenameColumn(
                name: "destination_inventory_location_id",
                table: "stock_transfers",
                newName: "destination_location_id");

            migrationBuilder.RenameIndex(
                name: "IX_stock_transfers_source_inventory_location_id",
                table: "stock_transfers",
                newName: "IX_stock_transfers_source_location_id");

            migrationBuilder.RenameIndex(
                name: "IX_stock_transfers_destination_inventory_location_id",
                table: "stock_transfers",
                newName: "IX_stock_transfers_destination_location_id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "stock_transfer_status_history",
                newName: "changed_at");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "stock_movements",
                newName: "occurred_at");

            migrationBuilder.RenameColumn(
                name: "movement_quantity",
                table: "stock_movements",
                newName: "quantity_change");

            migrationBuilder.RenameColumn(
                name: "adjustment_quantity",
                table: "stock_adjustment_lines",
                newName: "quantity_change");

            migrationBuilder.RenameColumn(
                name: "manufactured_date",
                table: "product_batches",
                newName: "manufactured_at");

            migrationBuilder.RenameColumn(
                name: "quantity_remaining",
                table: "inventory_cost_layers",
                newName: "total_cost");

            migrationBuilder.RenameColumn(
                name: "days_before_expiry",
                table: "expiry_discount_rule_tiers",
                newName: "starts_days_before_expiry");

            migrationBuilder.RenameColumn(
                name: "target_id",
                table: "discount_policy_targets",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "condition_sequence",
                table: "discount_policy_conditions",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "discount_code",
                table: "discount_policies",
                newName: "discount_policy_code");

            migrationBuilder.RenameIndex(
                name: "uq_discount_policies_tenant_id_discount_code",
                table: "discount_policies",
                newName: "uq_discount_policies_tenant_id_discount_policy_code");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "stocktake_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "completed_by_tenant_user_id",
                table: "stocktake_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "stocktake_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "generated_stock_adjustment_id",
                table: "stocktake_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_blind_count",
                table: "stocktake_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "stocktake_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "posted_at",
                table: "stocktake_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "posted_by_tenant_user_id",
                table: "stocktake_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "snapshot_at",
                table: "stocktake_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                table: "stocktake_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "started_by_tenant_user_id",
                table: "stocktake_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stocktake_status",
                table: "stocktake_sessions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "stocktake_type",
                table: "stocktake_sessions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "stocktake_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "stocktake_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_batch_id",
                table: "stocktake_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "counted_at",
                table: "stocktake_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "counted_by_tenant_user_id",
                table: "stocktake_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "counted_quantity",
                table: "stocktake_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "expected_quantity",
                table: "stocktake_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_note",
                table: "stocktake_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "line_number",
                table: "stocktake_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "stocktake_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stocktake_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "serial_number_id",
                table: "stocktake_line_serials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "count_result",
                table: "stocktake_line_serials",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "scanned_by_tenant_user_id",
                table: "stocktake_line_serials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scanned_serial_number",
                table: "stocktake_line_serials",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stocktake_line_serials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_tenant_user_id",
                table: "stock_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "stock_transfers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_tenant_user_id",
                table: "stock_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "stock_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "received_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "received_by_tenant_user_id",
                table: "stock_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "shipped_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shipped_by_tenant_user_id",
                table: "stock_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transfer_note",
                table: "stock_transfers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transfer_status",
                table: "stock_transfers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "stock_transfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_reason",
                table: "stock_transfer_status_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "changed_by_tenant_user_id",
                table: "stock_transfer_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "stock_transfer_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "stock_transfer_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_transfer_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"ALTER TABLE stock_transfer_lines ALTER COLUMN line_number TYPE integer USING CASE WHEN line_number ~ '^[0-9]+$' THEN line_number::integer ELSE 0 END;");

            migrationBuilder.AddColumn<decimal>(
                name: "damaged_quantity",
                table: "stock_transfer_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_note",
                table: "stock_transfer_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "stock_transfer_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "missing_quantity",
                table: "stock_transfer_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "product_batch_id",
                table: "stock_transfer_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "stock_transfer_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "received_quantity",
                table: "stock_transfer_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipped_quantity",
                table: "stock_transfer_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_transfer_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "stock_movements",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_balance_id",
                table: "stock_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "movement_note",
                table: "stock_movements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "movement_type",
                table: "stock_movements",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_after",
                table: "stock_movements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_before",
                table: "stock_movements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_cost",
                table: "stock_movements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                table: "stock_movements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_movement_serials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "reference_line_id",
                table: "stock_movement_references",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_movement_references",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_movement_cost_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "total_cost",
                table: "stock_movement_cost_allocations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                table: "stock_movement_cost_allocations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "stock_adjustment_reasons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "direction",
                table: "stock_adjustment_reasons",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_system_reason",
                table: "stock_adjustment_reasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "reason_name",
                table: "stock_adjustment_reasons",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_manager_approval",
                table: "stock_adjustment_reasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "stock_adjustment_reasons",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "stock_adjustment_reasons",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"ALTER TABLE stock_adjustment_lines ALTER COLUMN line_number TYPE integer USING CASE WHEN line_number ~ '^[0-9]+$' THEN line_number::integer ELSE 0 END;");

            migrationBuilder.AddColumn<string>(
                name: "line_note",
                table: "stock_adjustment_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_batch_id",
                table: "stock_adjustment_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "stock_adjustment_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_after",
                table: "stock_adjustment_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_before",
                table: "stock_adjustment_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "stock_adjustment_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                table: "stock_adjustment_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "serial_number",
                table: "serial_numbers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "serial_numbers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "current_inventory_balance_id",
                table: "serial_numbers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_batch_id",
                table: "serial_numbers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "serial_numbers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "received_at",
                table: "serial_numbers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "serial_status",
                table: "serial_numbers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "serial_numbers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_negative_stock",
                table: "product_inventory_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "costing_method",
                table: "product_inventory_settings",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_inventory_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_uom_id",
                table: "product_inventory_settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_stock_tracked",
                table: "product_inventory_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_batch_tracking",
                table: "product_inventory_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_expiry_tracking",
                table: "product_inventory_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_serial_tracking",
                table: "product_inventory_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_inventory_settings",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_inventory_settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_inventory_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "batch_number",
                table: "product_batches",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "first_received_at",
                table: "product_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_batch_number",
                table: "product_batches",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reservation_status",
                table: "inventory_reservations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "inventory_reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fulfillment_outlet_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "release_reason",
                table: "inventory_reservations",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "released_at",
                table: "inventory_reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reservation_source",
                table: "inventory_reservations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reserved_at",
                table: "inventory_reservations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "sales_channel_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_reference_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_reference_number",
                table: "inventory_reservations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"ALTER TABLE inventory_reservation_lines ALTER COLUMN line_number TYPE integer USING CASE WHEN line_number ~ '^[0-9]+$' THEN line_number::integer ELSE 0 END;");

            migrationBuilder.AddColumn<decimal>(
                name: "fulfilled_quantity",
                table: "inventory_reservation_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "inventory_reservation_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_reservation_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "released_quantity",
                table: "inventory_reservation_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "reserved_quantity",
                table: "inventory_reservation_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_reservation_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "allocated_at",
                table: "inventory_reservation_allocations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "allocation_status",
                table: "inventory_reservation_allocations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "fulfilled_quantity",
                table: "inventory_reservation_allocations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "released_at",
                table: "inventory_reservation_allocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "released_quantity",
                table: "inventory_reservation_allocations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "serial_number_id",
                table: "inventory_reservation_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_reservation_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_reorder_rules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "inventory_reorder_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_reorder",
                table: "inventory_reorder_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "lead_time_days",
                table: "inventory_reorder_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_stock_quantity",
                table: "inventory_reorder_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_stock_quantity",
                table: "inventory_reorder_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reorder_method",
                table: "inventory_reorder_rules",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "reorder_quantity",
                table: "inventory_reorder_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "safety_stock_quantity",
                table: "inventory_reorder_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "inventory_reorder_rules",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_product_id",
                table: "inventory_reorder_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_reorder_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "inventory_reorder_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "inventory_locations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "inventory_locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_quarantine_location",
                table: "inventory_locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_receiving_location",
                table: "inventory_locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_return_location",
                table: "inventory_locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable_location",
                table: "inventory_locations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "location_name",
                table: "inventory_locations",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "location_type",
                table: "inventory_locations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "parent_inventory_location_id",
                table: "inventory_locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "inventory_locations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "inventory_locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_cost",
                table: "inventory_cost_layers",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_balance_id",
                table: "inventory_cost_layers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "received_at",
                table: "inventory_cost_layers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "received_quantity",
                table: "inventory_cost_layers",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "remaining_quantity",
                table: "inventory_cost_layers",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "source_stock_movement_id",
                table: "inventory_cost_layers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "inventory_cost_layers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_cost_layers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_channel_allocations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "inventory_channel_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "inventory_channel_allocations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "safety_stock_quantity",
                table: "inventory_channel_allocations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "inventory_channel_allocations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_channel_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "inventory_channel_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "reserved_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_balances",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_batch_id",
                table: "inventory_balances",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "on_hand_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<decimal>(
                name: "damaged_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quarantine_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "inventory_balances",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_balances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "expiry_discount_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "expiry_discount_rules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "discount_policy_id",
                table: "expiry_discount_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_apply",
                table: "expiry_discount_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "require_manager_approval",
                table: "expiry_discount_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rule_name",
                table: "expiry_discount_rules",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "expiry_discount_rules",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "expiry_discount_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "expiry_discount_rule_tiers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                table: "expiry_discount_rule_tiers",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ends_days_before_expiry",
                table: "expiry_discount_rule_tiers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "expiry_discount_rule_tiers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "expiry_discount_rule_tiers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "expiry_discount_rule_tiers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "tier_name",
                table: "expiry_discount_rule_tiers",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "expiry_discount_rule_tiers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_batch_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "application_source",
                table: "expiry_discount_applications",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "application_status",
                table: "expiry_discount_applications",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "applied_from",
                table: "expiry_discount_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "applied_until",
                table: "expiry_discount_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approval_note",
                table: "expiry_discount_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "expiry_discount_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_tenant_user_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                table: "expiry_discount_applications",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "expiry_discount_rule_tier_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "discount_type_code",
                table: "discount_types",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "calculation_method",
                table: "discount_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discount_type_name",
                table: "discount_types",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_system_type",
                table: "discount_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "discount_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "collection_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "discount_policy_targets",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "target_mode",
                table: "discount_policy_targets",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "discount_policy_outlets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "discount_policy_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "discount_policy_outlets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "condition_group_no",
                table: "discount_policy_conditions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "condition_operator",
                table: "discount_policy_conditions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "condition_type",
                table: "discount_policy_conditions",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "condition_value_json",
                table: "discount_policy_conditions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "discount_policy_conditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_operator",
                table: "discount_policy_conditions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "discount_policy_conditions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "discount_policy_conditions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_conditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "discount_policy_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "discount_policy_channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_value",
                table: "discount_policies",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "discount_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "discount_policies",
                type: "char(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "discount_policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_policy_name",
                table: "discount_policies",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discount_scope",
                table: "discount_policies",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ends_at",
                table: "discount_policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stackable",
                table: "discount_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "max_discount_amount",
                table: "discount_policies",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_order_amount",
                table: "discount_policies",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_quantity",
                table: "discount_policies",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "discount_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "requires_manager_approval",
                table: "discount_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "stacking_group_code",
                table: "discount_policies",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "starts_at",
                table: "discount_policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "discount_policies",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "discount_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "variance_quantity",
                table: "stocktake_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                computedColumnSql: "counted_quantity - expected_quantity",
                stored: true);

            migrationBuilder.AddColumn<decimal>(
                name: "available_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                computedColumnSql: "on_hand_quantity - reserved_quantity - damaged_quantity - quarantine_quantity",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_generated_stock_adjustment_id",
                table: "stocktake_sessions",
                column: "generated_stock_adjustment_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_product_batch_id",
                table: "stocktake_lines",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_product_variant_id",
                table: "stocktake_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_stocktake_session_id",
                table: "stocktake_lines",
                column: "stocktake_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_lines_scope",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "stocktake_session_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_lines_tenant_id_stocktake_session_id_line_number",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "stocktake_session_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_line_serials_scanned_by_tenant_user_id",
                table: "stocktake_line_serials",
                column: "scanned_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_line_serials_stocktake_line_id",
                table: "stocktake_line_serials",
                column: "stocktake_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_line_serials_tenant_id_stocktake_line_id_scanned_serial_number",
                table: "stocktake_line_serials",
                columns: new[] { "tenant_id", "stocktake_line_id", "scanned_serial_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_transfers_locations",
                table: "stock_transfers",
                sql: "source_location_id <> destination_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_status_history_changed_by_tenant_user_id",
                table: "stock_transfer_status_history",
                column: "changed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_status_history_stock_transfer_id",
                table: "stock_transfer_status_history",
                column: "stock_transfer_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_status_history_tenant_id_stock_transfer_id_sequence_number",
                table: "stock_transfer_status_history",
                columns: new[] { "tenant_id", "stock_transfer_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_product_batch_id",
                table: "stock_transfer_lines",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_product_variant_id",
                table: "stock_transfer_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_stock_transfer_id",
                table: "stock_transfer_lines",
                column: "stock_transfer_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_lines_scope",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "stock_transfer_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_lines_tenant_id_stock_transfer_id_line_number",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "stock_transfer_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_inventory_balance_id",
                table: "stock_movements",
                column: "inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movements_tenant_id_idempotency_key",
                table: "stock_movements",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_costs",
                table: "stock_movements",
                sql: "(unit_cost IS NULL OR unit_cost >= 0) AND (total_cost IS NULL OR total_cost >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_quantity_after",
                table: "stock_movements",
                sql: "quantity_after = quantity_before + quantity_change");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_quantity_change",
                table: "stock_movements",
                sql: "quantity_change <> 0");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_serials_stock_movement_id",
                table: "stock_movement_serials",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_serials_tenant_id_stock_movement_id_serial_number_id",
                table: "stock_movement_serials",
                columns: new[] { "tenant_id", "stock_movement_id", "serial_number_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_references_stock_movement_id",
                table: "stock_movement_references",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_references_scope",
                table: "stock_movement_references",
                columns: new[] { "tenant_id", "stock_movement_id", "reference_type", "reference_id", "reference_line_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_cost_allocations_tenant_id_stock_movement_id_inventory_cost_layer_id",
                table: "stock_movement_cost_allocations",
                columns: new[] { "tenant_id", "stock_movement_id", "inventory_cost_layer_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movement_cost_allocations_costs",
                table: "stock_movement_cost_allocations",
                sql: "unit_cost >= 0 AND total_cost >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_product_batch_id",
                table: "stock_adjustment_lines",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_product_variant_id",
                table: "stock_adjustment_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_stock_adjustment_id",
                table: "stock_adjustment_lines",
                column: "stock_adjustment_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustment_lines_scope",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "stock_adjustment_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustment_lines_tenant_id_stock_adjustment_id_line_number",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "stock_adjustment_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_current_inventory_balance_id",
                table: "serial_numbers",
                column: "current_inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_product_batch_id",
                table: "serial_numbers",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_product_variant_id",
                table: "serial_numbers",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_serial_numbers_tenant_id_id",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_serial_numbers_tenant_id_product_id_serial_number",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "product_id", "serial_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_inventory_uom_id",
                table: "product_inventory_settings",
                column: "inventory_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_product_id",
                table: "product_inventory_settings",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_inventory_settings_tenant_id_product_id",
                table: "product_inventory_settings",
                columns: new[] { "tenant_id", "product_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_inventory_settings_tenant_id_product_variant_id",
                table: "product_inventory_settings",
                columns: new[] { "tenant_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_inventory_settings_batch_requires_stock",
                table: "product_inventory_settings",
                sql: "requires_batch_tracking = false OR is_stock_tracked = true");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_inventory_settings_expiry_requires_batch",
                table: "product_inventory_settings",
                sql: "requires_expiry_tracking = false OR requires_batch_tracking = true");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_inventory_settings_serial_requires_stock",
                table: "product_inventory_settings",
                sql: "requires_serial_tracking = false OR is_stock_tracked = true");

            migrationBuilder.CreateIndex(
                name: "uq_product_batches_tenant_id_product_id_batch_number",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_id", "batch_number" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_batches_tenant_id_product_id_product_variant_id_batch_number",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_id", "product_variant_id", "batch_number" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_batches_expiry_date",
                table: "product_batches",
                sql: "expiry_date IS NULL OR manufactured_at IS NULL OR expiry_date >= manufactured_at");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_customer_id",
                table: "inventory_reservations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_fulfillment_outlet_id",
                table: "inventory_reservations",
                column: "fulfillment_outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_sales_channel_id",
                table: "inventory_reservations",
                column: "sales_channel_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservations_expires_at",
                table: "inventory_reservations",
                sql: "expires_at IS NULL OR expires_at >= reserved_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservations_released_at",
                table: "inventory_reservations",
                sql: "released_at IS NULL OR released_at >= reserved_at");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_inventory_reservation_id",
                table: "inventory_reservation_lines",
                column: "inventory_reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_product_variant_id",
                table: "inventory_reservation_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_lines_tenant_id_inventory_reservation_id_line_number",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "inventory_reservation_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_quantities",
                table: "inventory_reservation_lines",
                sql: "line_number > 0 AND requested_quantity > 0 AND reserved_quantity >= 0 AND released_quantity >= 0 AND fulfilled_quantity >= 0 AND reserved_quantity <= requested_quantity AND released_quantity + fulfilled_quantity <= reserved_quantity");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_serial_number_id",
                table: "inventory_reservation_allocations",
                column: "serial_number_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_allocations_balance",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "inventory_reservation_line_id", "inventory_balance_id" },
                unique: true,
                filter: "serial_number_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_allocations_serial",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "inventory_reservation_line_id", "serial_number_id" },
                unique: true,
                filter: "serial_number_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_quantities",
                table: "inventory_reservation_allocations",
                sql: "allocated_quantity > 0 AND released_quantity >= 0 AND fulfilled_quantity >= 0 AND released_quantity + fulfilled_quantity <= allocated_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_released_at",
                table: "inventory_reservation_allocations",
                sql: "released_at IS NULL OR released_at >= allocated_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_serial_quantity",
                table: "inventory_reservation_allocations",
                sql: "serial_number_id IS NULL OR allocated_quantity = 1");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_inventory_location_id",
                table: "inventory_reorder_rules",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_product_variant_id",
                table: "inventory_reorder_rules",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reorder_rules_product",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "inventory_location_id", "product_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reorder_rules_variant",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "inventory_location_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_lead_time_days",
                table: "inventory_reorder_rules",
                sql: "lead_time_days IS NULL OR lead_time_days >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_min_max",
                table: "inventory_reorder_rules",
                sql: "max_stock_quantity IS NULL OR min_stock_quantity IS NULL OR max_stock_quantity >= min_stock_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules",
                sql: "reorder_point_quantity > 0 AND reorder_quantity > 0 AND safety_stock_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_parent_inventory_location_id",
                table: "inventory_locations",
                column: "parent_inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_locations_tenant_id_id",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_inventory_locations_tenant_id_outlet_id_location_code",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "outlet_id", "location_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_locations_parent_not_self",
                table: "inventory_locations",
                sql: "parent_inventory_location_id IS NULL OR parent_inventory_location_id <> id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_inventory_balance_id",
                table: "inventory_cost_layers",
                column: "inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_source_stock_movement_id",
                table: "inventory_cost_layers",
                column: "source_stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_tenant_id",
                table: "inventory_cost_layers",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_costs",
                table: "inventory_cost_layers",
                sql: "unit_cost >= 0 AND total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_received_quantity",
                table: "inventory_cost_layers",
                sql: "received_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity",
                table: "inventory_cost_layers",
                sql: "remaining_quantity >= 0 AND remaining_quantity <= received_quantity");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_inventory_location_id",
                table: "inventory_channel_allocations",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_product_id",
                table: "inventory_channel_allocations",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_product_variant_id",
                table: "inventory_channel_allocations",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_channel_allocations_product",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "inventory_location_id", "product_id", "sales_channel_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_channel_allocations_variant",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "inventory_location_id", "product_id", "product_variant_id", "sales_channel_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations",
                sql: "allocation_limit_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_channel_allocations_safety_stock_quantity",
                table: "inventory_channel_allocations",
                sql: "safety_stock_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_inventory_location_id",
                table: "inventory_balances",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_product_batch_id",
                table: "inventory_balances",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_product_variant_id",
                table: "inventory_balances",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_balances_scope",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "inventory_location_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_balances_damaged_quantity",
                table: "inventory_balances",
                sql: "damaged_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_balances_quarantine_quantity",
                table: "inventory_balances",
                sql: "quarantine_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rules_discount_policy_id",
                table: "expiry_discount_rules",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_tenant_id",
                table: "expiry_discount_rule_tiers",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_days",
                table: "expiry_discount_rule_tiers",
                sql: "starts_days_before_expiry >= 0 AND ends_days_before_expiry >= 0 AND starts_days_before_expiry >= ends_days_before_expiry");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_discount_percent",
                table: "expiry_discount_rule_tiers",
                sql: "discount_percent >= 0 AND discount_percent <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_sort_order",
                table: "expiry_discount_rule_tiers",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_approved_by_tenant_user_id",
                table: "expiry_discount_applications",
                column: "approved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_tier_id",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_outlet_id",
                table: "expiry_discount_applications",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_product_batch_id",
                table: "expiry_discount_applications",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_applications_active_batch_outlet",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "product_batch_id", "outlet_id" },
                unique: true,
                filter: "application_status = 'ACTIVE'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_applications_discount_percent",
                table: "expiry_discount_applications",
                sql: "discount_percent >= 0 AND discount_percent <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_applications_period",
                table: "expiry_discount_applications",
                sql: "applied_until IS NULL OR applied_from IS NULL OR applied_until >= applied_from");

            migrationBuilder.CreateIndex(
                name: "uq_discount_types_discount_type_code",
                table: "discount_types",
                column: "discount_type_code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_types_calculation_method",
                table: "discount_types",
                sql: "calculation_method IN ('PERCENTAGE', 'FIXED_AMOUNT', 'FIXED_PRICE', 'BUY_X_GET_Y')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_types_status",
                table: "discount_types",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_brand_id",
                table: "discount_policy_targets",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_category_id",
                table: "discount_policy_targets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_collection_id",
                table: "discount_policy_targets",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_discount_policy_id",
                table: "discount_policy_targets",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_product_id",
                table: "discount_policy_targets",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_product_variant_id",
                table: "discount_policy_targets",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id",
                table: "discount_policy_targets",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_targets_one_target",
                table: "discount_policy_targets",
                sql: "((target_type = 'PRODUCT' AND product_id IS NOT NULL AND product_variant_id IS NULL AND category_id IS NULL AND brand_id IS NULL AND collection_id IS NULL) OR (target_type = 'PRODUCT_VARIANT' AND product_id IS NULL AND product_variant_id IS NOT NULL AND category_id IS NULL AND brand_id IS NULL AND collection_id IS NULL) OR (target_type = 'CATEGORY' AND product_id IS NULL AND product_variant_id IS NULL AND category_id IS NOT NULL AND brand_id IS NULL AND collection_id IS NULL) OR (target_type = 'BRAND' AND product_id IS NULL AND product_variant_id IS NULL AND category_id IS NULL AND brand_id IS NOT NULL AND collection_id IS NULL) OR (target_type = 'COLLECTION' AND product_id IS NULL AND product_variant_id IS NULL AND category_id IS NULL AND brand_id IS NULL AND collection_id IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_targets_target_mode",
                table: "discount_policy_targets",
                sql: "target_mode IN ('INCLUDE', 'EXCLUDE')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_discount_policy_id",
                table: "discount_policy_outlets",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_outlets_tenant_id_discount_policy_id_outlet_id",
                table: "discount_policy_outlets",
                columns: new[] { "tenant_id", "discount_policy_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_tenant_id",
                table: "discount_policy_conditions",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_conditions_condition_group_no",
                table: "discount_policy_conditions",
                sql: "condition_group_no > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_conditions_group_operator",
                table: "discount_policy_conditions",
                sql: "group_operator IN ('AND', 'OR')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_conditions_sort_order",
                table: "discount_policy_conditions",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_discount_policy_id",
                table: "discount_policy_channels",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_channels_tenant_id_discount_policy_id_sales_channel_id",
                table: "discount_policy_channels",
                columns: new[] { "tenant_id", "discount_policy_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discount_policies_created_by_tenant_user_id",
                table: "discount_policies",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policies_currency_code",
                table: "discount_policies",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policies_updated_by_tenant_user_id",
                table: "discount_policies",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_amounts",
                table: "discount_policies",
                sql: "(max_discount_amount IS NULL OR max_discount_amount >= 0) AND (min_order_amount IS NULL OR min_order_amount >= 0) AND (min_quantity IS NULL OR min_quantity >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_period",
                table: "discount_policies",
                sql: "ends_at IS NULL OR starts_at IS NULL OR ends_at >= starts_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_priority",
                table: "discount_policies",
                sql: "priority >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_status",
                table: "discount_policies",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policies_created_by_tenant_user_id_tenant_users",
                table: "discount_policies",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policies_currency_code_currencies",
                table: "discount_policies",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policies_updated_by_tenant_user_id_tenant_users",
                table: "discount_policies",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_tenant_id_tenants",
                table: "discount_policy_channels",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_conditions_tenant_id_tenants",
                table: "discount_policy_conditions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_tenant_id_tenants",
                table: "discount_policy_outlets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_brand_id_brands",
                table: "discount_policy_targets",
                column: "brand_id",
                principalTable: "brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_category_id_categories",
                table: "discount_policy_targets",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_collection_id_collections",
                table: "discount_policy_targets",
                column: "collection_id",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_product_id_products",
                table: "discount_policy_targets",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_product_variant_id_product_variants",
                table: "discount_policy_targets",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_tenant_id_tenants",
                table: "discount_policy_targets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_approved_by_tenant_user_id_tenant_users",
                table: "expiry_discount_applications",
                column: "approved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_tier_id",
                principalTable: "expiry_discount_rule_tiers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_outlet_id_outlets",
                table: "expiry_discount_applications",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_tenant_id_tenants",
                table: "expiry_discount_applications",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rule_tiers_tenant_id_tenants",
                table: "expiry_discount_rule_tiers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rules_discount_policy_id_discount_policies",
                table: "expiry_discount_rules",
                column: "discount_policy_id",
                principalTable: "discount_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_product_batch_id_product_batches",
                table: "inventory_balances",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_product_variant_id_product_variants",
                table: "inventory_balances",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_tenant_id_tenants",
                table: "inventory_balances",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_product_id_products",
                table: "inventory_channel_allocations",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_product_variant_id_product_variants",
                table: "inventory_channel_allocations",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_tenant_id_tenants",
                table: "inventory_channel_allocations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_cost_layers_inventory_balance_id_inventory_balances",
                table: "inventory_cost_layers",
                column: "inventory_balance_id",
                principalTable: "inventory_balances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_cost_layers_source_stock_movement_id_stock_movements",
                table: "inventory_cost_layers",
                column: "source_stock_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_cost_layers_tenant_id_tenants",
                table: "inventory_cost_layers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_locations_parent_inventory_location_id_inventory_locations",
                table: "inventory_locations",
                column: "parent_inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_product_variant_id_product_variants",
                table: "inventory_reorder_rules",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_tenant_id_tenants",
                table: "inventory_reorder_rules",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_serial_number_id_serial_numbers",
                table: "inventory_reservation_allocations",
                column: "serial_number_id",
                principalTable: "serial_numbers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_tenant_id_tenants",
                table: "inventory_reservation_allocations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_lines_product_variant_id_product_variants",
                table: "inventory_reservation_lines",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_lines_tenant_id_tenants",
                table: "inventory_reservation_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_customer_id_customers",
                table: "inventory_reservations",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_fulfillment_outlet_id_outlets",
                table: "inventory_reservations",
                column: "fulfillment_outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_sales_channel_id_sales_channels",
                table: "inventory_reservations",
                column: "sales_channel_id",
                principalTable: "sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_tenant_id_tenants",
                table: "product_batches",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_inventory_uom_id_unit_of_measures",
                table: "product_inventory_settings",
                column: "inventory_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_tenant_id_tenants",
                table: "product_inventory_settings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_current_inventory_balance_id_inventory_balances",
                table: "serial_numbers",
                column: "current_inventory_balance_id",
                principalTable: "inventory_balances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_product_batch_id_product_batches",
                table: "serial_numbers",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_product_variant_id_product_variants",
                table: "serial_numbers",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_tenant_id_tenants",
                table: "serial_numbers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_batch_id_product_batches",
                table: "stock_adjustment_lines",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_variant_id_product_variants",
                table: "stock_adjustment_lines",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_tenant_id_tenants",
                table: "stock_adjustment_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_cost_allocations_tenant_id_tenants",
                table: "stock_movement_cost_allocations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_references_tenant_id_tenants",
                table: "stock_movement_references",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_serials_tenant_id_tenants",
                table: "stock_movement_serials",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_inventory_balance_id_inventory_balances",
                table: "stock_movements",
                column: "inventory_balance_id",
                principalTable: "inventory_balances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_product_batch_id_product_batches",
                table: "stock_transfer_lines",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_product_variant_id_product_variants",
                table: "stock_transfer_lines",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_tenant_id_tenants",
                table: "stock_transfer_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_status_history_changed_by_tenant_user_id_tenant_users",
                table: "stock_transfer_status_history",
                column: "changed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_status_history_tenant_id_tenants",
                table: "stock_transfer_status_history",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_destination_location_id_inventory_locations",
                table: "stock_transfers",
                column: "destination_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_source_location_id_inventory_locations",
                table: "stock_transfers",
                column: "source_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_tenant_id_tenants",
                table: "stock_transfers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_line_serials_scanned_by_tenant_user_id_tenant_users",
                table: "stocktake_line_serials",
                column: "scanned_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_line_serials_tenant_id_tenants",
                table: "stocktake_line_serials",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_product_batch_id_product_batches",
                table: "stocktake_lines",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_product_variant_id_product_variants",
                table: "stocktake_lines",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_tenant_id_tenants",
                table: "stocktake_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_generated_stock_adjustment_id_stock_adjustments",
                table: "stocktake_sessions",
                column: "generated_stock_adjustment_id",
                principalTable: "stock_adjustments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_tenant_id_tenants",
                table: "stocktake_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_policies_created_by_tenant_user_id_tenant_users",
                table: "discount_policies");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policies_currency_code_currencies",
                table: "discount_policies");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policies_updated_by_tenant_user_id_tenant_users",
                table: "discount_policies");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_tenant_id_tenants",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_conditions_tenant_id_tenants",
                table: "discount_policy_conditions");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_tenant_id_tenants",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_brand_id_brands",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_category_id_categories",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_collection_id_collections",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_product_id_products",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_product_variant_id_product_variants",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_tenant_id_tenants",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_approved_by_tenant_user_id_tenant_users",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_outlet_id_outlets",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_tenant_id_tenants",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rule_tiers_tenant_id_tenants",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rules_discount_policy_id_discount_policies",
                table: "expiry_discount_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_batch_id_product_batches",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_variant_id_product_variants",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_tenant_id_tenants",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_product_id_products",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_product_variant_id_product_variants",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_tenant_id_tenants",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_inventory_balance_id_inventory_balances",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_source_stock_movement_id_stock_movements",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_tenant_id_tenants",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_parent_inventory_location_id_inventory_locations",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_product_variant_id_product_variants",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_tenant_id_tenants",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_serial_number_id_serial_numbers",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_tenant_id_tenants",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_product_variant_id_product_variants",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_tenant_id_tenants",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservations_customer_id_customers",
                table: "inventory_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservations_fulfillment_outlet_id_outlets",
                table: "inventory_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservations_sales_channel_id_sales_channels",
                table: "inventory_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_tenant_id_tenants",
                table: "product_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_inventory_uom_id_unit_of_measures",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_tenant_id_tenants",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_current_inventory_balance_id_inventory_balances",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_batch_id_product_batches",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_variant_id_product_variants",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_tenant_id_tenants",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_batch_id_product_batches",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_variant_id_product_variants",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_tenant_id_tenants",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_cost_allocations_tenant_id_tenants",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_references_tenant_id_tenants",
                table: "stock_movement_references");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_serials_tenant_id_tenants",
                table: "stock_movement_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_inventory_balance_id_inventory_balances",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_batch_id_product_batches",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_variant_id_product_variants",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_tenant_id_tenants",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_status_history_changed_by_tenant_user_id_tenant_users",
                table: "stock_transfer_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_status_history_tenant_id_tenants",
                table: "stock_transfer_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_destination_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_source_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_tenant_id_tenants",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_line_serials_scanned_by_tenant_user_id_tenant_users",
                table: "stocktake_line_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_line_serials_tenant_id_tenants",
                table: "stocktake_line_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_batch_id_product_batches",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_variant_id_product_variants",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_tenant_id_tenants",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_generated_stock_adjustment_id_stock_adjustments",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_tenant_id_tenants",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_generated_stock_adjustment_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_product_batch_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_product_variant_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_stocktake_session_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_lines_scope",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_lines_tenant_id_stocktake_session_id_line_number",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_line_serials_scanned_by_tenant_user_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_line_serials_stocktake_line_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_line_serials_tenant_id_stocktake_line_id_scanned_serial_number",
                table: "stocktake_line_serials");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_transfers_locations",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_status_history_changed_by_tenant_user_id",
                table: "stock_transfer_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_status_history_stock_transfer_id",
                table: "stock_transfer_status_history");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_status_history_tenant_id_stock_transfer_id_sequence_number",
                table: "stock_transfer_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_product_batch_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_product_variant_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_stock_transfer_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_lines_scope",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_lines_tenant_id_stock_transfer_id_line_number",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_inventory_balance_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "uq_stock_movements_tenant_id_idempotency_key",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_costs",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_quantity_after",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_quantity_change",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_serials_stock_movement_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_serials_tenant_id_stock_movement_id_serial_number_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_references_stock_movement_id",
                table: "stock_movement_references");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_references_scope",
                table: "stock_movement_references");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_cost_allocations_tenant_id_stock_movement_id_inventory_cost_layer_id",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movement_cost_allocations_costs",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_product_batch_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_product_variant_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_stock_adjustment_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "uq_stock_adjustment_lines_scope",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "uq_stock_adjustment_lines_tenant_id_stock_adjustment_id_line_number",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_current_inventory_balance_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_product_batch_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_product_variant_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "uq_serial_numbers_tenant_id_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "uq_serial_numbers_tenant_id_product_id_serial_number",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_product_inventory_settings_inventory_uom_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "IX_product_inventory_settings_product_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "uq_product_inventory_settings_tenant_id_product_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "uq_product_inventory_settings_tenant_id_product_variant_id",
                table: "product_inventory_settings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_inventory_settings_batch_requires_stock",
                table: "product_inventory_settings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_inventory_settings_expiry_requires_batch",
                table: "product_inventory_settings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_inventory_settings_serial_requires_stock",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "uq_product_batches_tenant_id_product_id_batch_number",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "uq_product_batches_tenant_id_product_id_product_variant_id_batch_number",
                table: "product_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_batches_expiry_date",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_customer_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_fulfillment_outlet_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_sales_channel_id",
                table: "inventory_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservations_expires_at",
                table: "inventory_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservations_released_at",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_inventory_reservation_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_product_variant_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservation_lines_tenant_id_inventory_reservation_id_line_number",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_quantities",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_allocations_serial_number_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservation_allocations_balance",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservation_allocations_serial",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_quantities",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_released_at",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_serial_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_inventory_location_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_product_variant_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reorder_rules_product",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reorder_rules_variant",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_lead_time_days",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_min_max",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_locations_parent_inventory_location_id",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_locations_tenant_id_id",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_locations_tenant_id_outlet_id_location_code",
                table: "inventory_locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_locations_parent_not_self",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_cost_layers_inventory_balance_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "IX_inventory_cost_layers_source_stock_movement_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "IX_inventory_cost_layers_tenant_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_costs",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_received_quantity",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_inventory_location_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_product_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_product_variant_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_channel_allocations_product",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_channel_allocations_variant",
                table: "inventory_channel_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_channel_allocations_safety_stock_quantity",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_inventory_location_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_product_batch_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_product_variant_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "uq_inventory_balances_scope",
                table: "inventory_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_balances_damaged_quantity",
                table: "inventory_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_balances_quarantine_quantity",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rules_discount_policy_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rule_tiers_tenant_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_days",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_discount_percent",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_sort_order",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_approved_by_tenant_user_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_tier_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_outlet_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_product_batch_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "uq_expiry_discount_applications_active_batch_outlet",
                table: "expiry_discount_applications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_applications_discount_percent",
                table: "expiry_discount_applications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_applications_period",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "uq_discount_types_discount_type_code",
                table: "discount_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_types_calculation_method",
                table: "discount_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_types_status",
                table: "discount_types");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_brand_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_category_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_collection_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_discount_policy_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_product_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_product_variant_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id",
                table: "discount_policy_targets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_targets_one_target",
                table: "discount_policy_targets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_targets_target_mode",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_outlets_discount_policy_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_outlets_tenant_id_discount_policy_id_outlet_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_conditions_tenant_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_conditions_condition_group_no",
                table: "discount_policy_conditions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_conditions_group_operator",
                table: "discount_policy_conditions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_conditions_sort_order",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_channels_discount_policy_id",
                table: "discount_policy_channels");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_channels_tenant_id_discount_policy_id_sales_channel_id",
                table: "discount_policy_channels");

            migrationBuilder.DropIndex(
                name: "IX_discount_policies_created_by_tenant_user_id",
                table: "discount_policies");

            migrationBuilder.DropIndex(
                name: "IX_discount_policies_currency_code",
                table: "discount_policies");

            migrationBuilder.DropIndex(
                name: "IX_discount_policies_updated_by_tenant_user_id",
                table: "discount_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_amounts",
                table: "discount_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_period",
                table: "discount_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_priority",
                table: "discount_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_status",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "variance_quantity",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "available_quantity",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "completed_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "generated_stock_adjustment_id",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "is_blind_count",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "posted_at",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "posted_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "snapshot_at",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "started_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "stocktake_status",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "stocktake_type",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropColumn(
                name: "counted_at",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "counted_by_tenant_user_id",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "counted_quantity",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "expected_quantity",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "line_note",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stocktake_lines");

            migrationBuilder.DropColumn(
                name: "count_result",
                table: "stocktake_line_serials");

            migrationBuilder.DropColumn(
                name: "scanned_by_tenant_user_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropColumn(
                name: "scanned_serial_number",
                table: "stocktake_line_serials");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "approved_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "cancelled_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "received_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "shipped_at",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "shipped_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "transfer_note",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "transfer_status",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropColumn(
                name: "change_reason",
                table: "stock_transfer_status_history");

            migrationBuilder.DropColumn(
                name: "changed_by_tenant_user_id",
                table: "stock_transfer_status_history");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "stock_transfer_status_history");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "stock_transfer_status_history");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_transfer_status_history");

            migrationBuilder.DropColumn(
                name: "damaged_quantity",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "line_note",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "missing_quantity",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "product_batch_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "received_quantity",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "shipped_quantity",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "inventory_balance_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "movement_note",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "movement_type",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "quantity_after",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "quantity_before",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "total_cost",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_movement_serials");

            migrationBuilder.DropColumn(
                name: "reference_line_id",
                table: "stock_movement_references");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_movement_references");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropColumn(
                name: "total_cost",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "direction",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "is_system_reason",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "reason_name",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "requires_manager_approval",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "status",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropColumn(
                name: "line_note",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "product_batch_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "quantity_after",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "quantity_before",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                table: "stock_adjustment_lines");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "current_inventory_balance_id",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "product_batch_id",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "serial_status",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "serial_numbers");

            migrationBuilder.DropColumn(
                name: "allow_negative_stock",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "costing_method",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "inventory_uom_id",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "is_stock_tracked",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "requires_batch_tracking",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "requires_expiry_tracking",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "requires_serial_tracking",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_inventory_settings");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_batches");

            migrationBuilder.DropColumn(
                name: "first_received_at",
                table: "product_batches");

            migrationBuilder.DropColumn(
                name: "supplier_batch_number",
                table: "product_batches");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_batches");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "fulfillment_outlet_id",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "release_reason",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "released_at",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "reservation_source",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "reserved_at",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "sales_channel_id",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "source_reference_id",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "source_reference_number",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "inventory_reservations");

            migrationBuilder.DropColumn(
                name: "fulfilled_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "inventory_reservation_lines");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropColumn(
                name: "released_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropColumn(
                name: "reserved_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropColumn(
                name: "allocated_at",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "allocation_status",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "fulfilled_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "released_at",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "released_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "serial_number_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "is_auto_reorder",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "lead_time_days",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "max_stock_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "min_stock_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "reorder_method",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "reorder_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "safety_stock_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "status",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "supplier_product_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "is_quarantine_location",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "is_receiving_location",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "is_return_location",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "is_sellable_location",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "location_name",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "location_type",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "parent_inventory_location_id",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "inventory_locations");

            migrationBuilder.DropColumn(
                name: "inventory_balance_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "received_quantity",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "remaining_quantity",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "source_stock_movement_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "status",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "inventory_channel_allocations");

            migrationBuilder.DropColumn(
                name: "safety_stock_quantity",
                table: "inventory_channel_allocations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "inventory_channel_allocations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropColumn(
                name: "damaged_quantity",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "quarantine_quantity",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "description",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "discount_policy_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "is_auto_apply",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "require_manager_approval",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "rule_name",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "status",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "ends_days_before_expiry",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "status",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "tier_name",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropColumn(
                name: "application_source",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "application_status",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "applied_from",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "applied_until",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "approval_note",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "approved_by_tenant_user_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "expiry_discount_rule_tier_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropColumn(
                name: "calculation_method",
                table: "discount_types");

            migrationBuilder.DropColumn(
                name: "discount_type_name",
                table: "discount_types");

            migrationBuilder.DropColumn(
                name: "is_system_type",
                table: "discount_types");

            migrationBuilder.DropColumn(
                name: "status",
                table: "discount_types");

            migrationBuilder.DropColumn(
                name: "brand_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "collection_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "status",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "target_mode",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_targets");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropColumn(
                name: "condition_group_no",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "condition_operator",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "condition_type",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "condition_value_json",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "group_operator",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "discount_policy_channels");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "discount_policy_channels");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "discount_policy_channels");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "description",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "discount_policy_name",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "discount_scope",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "ends_at",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "is_stackable",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "max_discount_amount",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "min_order_amount",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "min_quantity",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "requires_manager_approval",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "stacking_group_code",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "starts_at",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "status",
                table: "discount_policies");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "discount_policies");

            migrationBuilder.RenameColumn(
                name: "scanned_at",
                table: "stocktake_line_serials",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "source_location_id",
                table: "stock_transfers",
                newName: "source_inventory_location_id");

            migrationBuilder.RenameColumn(
                name: "destination_location_id",
                table: "stock_transfers",
                newName: "destination_inventory_location_id");

            migrationBuilder.RenameIndex(
                name: "IX_stock_transfers_source_location_id",
                table: "stock_transfers",
                newName: "IX_stock_transfers_source_inventory_location_id");

            migrationBuilder.RenameIndex(
                name: "IX_stock_transfers_destination_location_id",
                table: "stock_transfers",
                newName: "IX_stock_transfers_destination_inventory_location_id");

            migrationBuilder.RenameColumn(
                name: "changed_at",
                table: "stock_transfer_status_history",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "quantity_change",
                table: "stock_movements",
                newName: "movement_quantity");

            migrationBuilder.RenameColumn(
                name: "occurred_at",
                table: "stock_movements",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "quantity_change",
                table: "stock_adjustment_lines",
                newName: "adjustment_quantity");

            migrationBuilder.RenameColumn(
                name: "manufactured_at",
                table: "product_batches",
                newName: "manufactured_date");

            migrationBuilder.RenameColumn(
                name: "total_cost",
                table: "inventory_cost_layers",
                newName: "quantity_remaining");

            migrationBuilder.RenameColumn(
                name: "starts_days_before_expiry",
                table: "expiry_discount_rule_tiers",
                newName: "days_before_expiry");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "discount_policy_targets",
                newName: "target_id");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "discount_policy_conditions",
                newName: "condition_sequence");

            migrationBuilder.RenameColumn(
                name: "discount_policy_code",
                table: "discount_policies",
                newName: "discount_code");

            migrationBuilder.RenameIndex(
                name: "uq_discount_policies_tenant_id_discount_policy_code",
                table: "discount_policies",
                newName: "uq_discount_policies_tenant_id_discount_code");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "stocktake_sessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "stocktake_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_batch_id",
                table: "stocktake_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "serial_number_id",
                table: "stocktake_line_serials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "stock_transfers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "stock_transfer_status_history",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "line_number",
                table: "stock_transfer_lines",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "stock_movement_serials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "stock_movement_references",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "allocated_cost_amount",
                table: "stock_movement_cost_allocations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "stock_movement_cost_allocations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "stock_adjustment_reasons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "stock_adjustment_reasons",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "line_number",
                table: "stock_adjustment_lines",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "serial_number",
                table: "serial_numbers",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "batch_number",
                table: "product_batches",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "reservation_status",
                table: "inventory_reservations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "line_number",
                table: "inventory_reservation_lines",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_reorder_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "inventory_locations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "inventory_locations",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_cost",
                table: "inventory_cost_layers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<Guid>(
                name: "product_batch_id",
                table: "inventory_cost_layers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_channel_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "reserved_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "inventory_balances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_batch_id",
                table: "inventory_balances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "on_hand_quantity",
                table: "inventory_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "expiry_discount_rules",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_value",
                table: "expiry_discount_rule_tiers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_batch_id",
                table: "expiry_discount_applications",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "discount_type_code",
                table: "discount_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "discount_types",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "discount_types",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "discount_policy_outlets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_value",
                table: "discount_policies",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "discount_policies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_lines_stocktake_session_id_product_id_product_variant_id_product_batch_id",
                table: "stocktake_lines",
                columns: new[] { "stocktake_session_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_line_serials_stocktake_line_id_serial_number_id",
                table: "stocktake_line_serials",
                columns: new[] { "stocktake_line_id", "serial_number_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_transfers_source_inventory_location_id_destination_in~",
                table: "stock_transfers",
                sql: "source_inventory_location_id <> destination_inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_status_history_stock_transfer_id_sequence_number",
                table: "stock_transfer_status_history",
                columns: new[] { "stock_transfer_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_lines_stock_transfer_id_line_number",
                table: "stock_transfer_lines",
                columns: new[] { "stock_transfer_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_transfer_lines_requested_quantity",
                table: "stock_transfer_lines",
                sql: "requested_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_movement_quantity",
                table: "stock_movements",
                sql: "movement_quantity <> 0");

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_serials_stock_movement_id_serial_number_id",
                table: "stock_movement_serials",
                columns: new[] { "stock_movement_id", "serial_number_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_references_stock_movement_id_reference_type_reference_id",
                table: "stock_movement_references",
                columns: new[] { "stock_movement_id", "reference_type", "reference_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movement_cost_allocations_allocated_cost_amount",
                table: "stock_movement_cost_allocations",
                sql: "allocated_cost_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustment_lines_stock_adjustment_id_line_number",
                table: "stock_adjustment_lines",
                columns: new[] { "stock_adjustment_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_adjustment_lines_adjustment_quantity",
                table: "stock_adjustment_lines",
                sql: "adjustment_quantity <> 0");

            migrationBuilder.CreateIndex(
                name: "uq_serial_numbers_tenant_id_serial_number",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "serial_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_inventory_settings_product_id_product_variant_id",
                table: "product_inventory_settings",
                columns: new[] { "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_batches_tenant_id_product_id_product_variant_id_batch_number",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_id", "product_variant_id", "batch_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_batches_expiry_date_manufactured_date",
                table: "product_batches",
                sql: "expiry_date IS NULL OR expiry_date >= manufactured_date");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservations_reservation_status",
                table: "inventory_reservations",
                sql: "reservation_status IN ('PENDING', 'CONFIRMED', 'RELEASED', 'EXPIRED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_lines_inventory_reservation_id_line_number",
                table: "inventory_reservation_lines",
                columns: new[] { "inventory_reservation_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_requested_quantity",
                table: "inventory_reservation_lines",
                sql: "requested_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_allocated_quantity",
                table: "inventory_reservation_allocations",
                sql: "allocated_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reorder_rules_inventory_location_id_product_id_product_variant_id",
                table: "inventory_reorder_rules",
                columns: new[] { "inventory_location_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_reorder_point_quantity",
                table: "inventory_reorder_rules",
                sql: "reorder_point_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_locations_tenant_id_location_code",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "location_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_product_batch_id",
                table: "inventory_cost_layers",
                column: "product_batch_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_quantity_remaining",
                table: "inventory_cost_layers",
                sql: "quantity_remaining >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_unit_cost",
                table: "inventory_cost_layers",
                sql: "unit_cost >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_channel_allocations_inventory_location_id_product_id_product_variant_id_sales_channel_id",
                table: "inventory_channel_allocations",
                columns: new[] { "inventory_location_id", "product_id", "product_variant_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations",
                sql: "allocation_limit_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_balances_inventory_location_id_product_id_product_variant_id_product_batch_id",
                table: "inventory_balances",
                columns: new[] { "inventory_location_id", "product_id", "product_variant_id", "product_batch_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_balances_on_hand_quantity",
                table: "inventory_balances",
                sql: "on_hand_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_days_before_expiry",
                table: "expiry_discount_rule_tiers",
                sql: "days_before_expiry >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_discount_value",
                table: "expiry_discount_rule_tiers",
                sql: "discount_value >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_applications_product_batch_id_expiry_discount_rule_id",
                table: "expiry_discount_applications",
                columns: new[] { "product_batch_id", "expiry_discount_rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_types_tenant_id_discount_type_code",
                table: "discount_types",
                columns: new[] { "tenant_id", "discount_type_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_targets_discount_policy_id_target_type_target_id",
                table: "discount_policy_targets",
                columns: new[] { "discount_policy_id", "target_type", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_outlets_discount_policy_id_outlet_id",
                table: "discount_policy_outlets",
                columns: new[] { "discount_policy_id", "outlet_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_conditions_condition_sequence",
                table: "discount_policy_conditions",
                sql: "condition_sequence > 0");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_channels_discount_policy_id_sales_channel_id",
                table: "discount_policy_channels",
                columns: new[] { "discount_policy_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_types_tenant_id_tenants",
                table: "discount_types",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_cost_layers_product_batch_id_product_batches",
                table: "inventory_cost_layers",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_destination_inventory_location_id_inventory_locations",
                table: "stock_transfers",
                column: "destination_inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_source_inventory_location_id_inventory_locations",
                table: "stock_transfers",
                column: "source_inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
