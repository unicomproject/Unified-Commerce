using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInventoryAdjustmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_costs",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movement_cost_allocations_costs",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_quantities",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_quantities",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_costs",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity",
                table: "inventory_cost_layers");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "stock_adjustments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "stock_adjustments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "product_batches",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<long>(
                name: "row_version",
                table: "inventory_balances",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 1L);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_total_cost",
                table: "stock_movements",
                sql: "total_cost IS NULL OR total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_unit_cost",
                table: "stock_movements",
                sql: "unit_cost IS NULL OR unit_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movement_cost_allocations_total_cost",
                table: "stock_movement_cost_allocations",
                sql: "total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movement_cost_allocations_unit_cost",
                table: "stock_movement_cost_allocations",
                sql: "unit_cost >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_created_by_tenant_user_id",
                table: "stock_adjustments",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_updated_by_tenant_user_id",
                table: "stock_adjustments",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_fulfilled_quantity",
                table: "inventory_reservation_lines",
                sql: "fulfilled_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_line_number",
                table: "inventory_reservation_lines",
                sql: "line_number > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_release_fulfilled_rule",
                table: "inventory_reservation_lines",
                sql: "released_quantity + fulfilled_quantity <= reserved_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_released_quantity",
                table: "inventory_reservation_lines",
                sql: "released_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_requested_quantity",
                table: "inventory_reservation_lines",
                sql: "requested_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_reserved_quantity",
                table: "inventory_reservation_lines",
                sql: "reserved_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_reserved_requested_rule",
                table: "inventory_reservation_lines",
                sql: "reserved_quantity <= requested_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_allocated_quantity",
                table: "inventory_reservation_allocations",
                sql: "allocated_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_fulfilled_quantity",
                table: "inventory_reservation_allocations",
                sql: "fulfilled_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_release_fulfilled_rule",
                table: "inventory_reservation_allocations",
                sql: "released_quantity + fulfilled_quantity <= allocated_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_released_quantity",
                table: "inventory_reservation_allocations",
                sql: "released_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_reorder_point_quantity",
                table: "inventory_reorder_rules",
                sql: "reorder_point_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_reorder_quantity",
                table: "inventory_reorder_rules",
                sql: "reorder_quantity IS NULL OR reorder_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_safety_stock_quantity",
                table: "inventory_reorder_rules",
                sql: "safety_stock_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity_max",
                table: "inventory_cost_layers",
                sql: "remaining_quantity <= received_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity_min",
                table: "inventory_cost_layers",
                sql: "remaining_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_total_cost",
                table: "inventory_cost_layers",
                sql: "total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_unit_cost",
                table: "inventory_cost_layers",
                sql: "unit_cost >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustments_created_by_tenant_user_id_tenant_users",
                table: "stock_adjustments",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustments_updated_by_tenant_user_id_tenant_users",
                table: "stock_adjustments",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustments_created_by_tenant_user_id_tenant_users",
                table: "stock_adjustments");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustments_updated_by_tenant_user_id_tenant_users",
                table: "stock_adjustments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_total_cost",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_unit_cost",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movement_cost_allocations_total_cost",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movement_cost_allocations_unit_cost",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustments_created_by_tenant_user_id",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustments_updated_by_tenant_user_id",
                table: "stock_adjustments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_fulfilled_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_line_number",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_release_fulfilled_rule",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_released_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_requested_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_reserved_quantity",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_lines_reserved_requested_rule",
                table: "inventory_reservation_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_allocated_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_fulfilled_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_release_fulfilled_rule",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reservation_allocations_released_quantity",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_reorder_point_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_reorder_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_safety_stock_quantity",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity_max",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity_min",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_total_cost",
                table: "inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_cost_layers_unit_cost",
                table: "inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "stock_adjustments");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "stock_adjustments");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "product_batches",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<long>(
                name: "row_version",
                table: "inventory_balances",
                type: "bigint",
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_costs",
                table: "stock_movements",
                sql: "(unit_cost IS NULL OR unit_cost >= 0) AND (total_cost IS NULL OR total_cost >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movement_cost_allocations_costs",
                table: "stock_movement_cost_allocations",
                sql: "unit_cost >= 0 AND total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_lines_quantities",
                table: "inventory_reservation_lines",
                sql: "line_number > 0 AND requested_quantity > 0 AND reserved_quantity >= 0 AND released_quantity >= 0 AND fulfilled_quantity >= 0 AND reserved_quantity <= requested_quantity AND released_quantity + fulfilled_quantity <= reserved_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reservation_allocations_quantities",
                table: "inventory_reservation_allocations",
                sql: "allocated_quantity > 0 AND released_quantity >= 0 AND fulfilled_quantity >= 0 AND released_quantity + fulfilled_quantity <= allocated_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules",
                sql: "(reorder_quantity IS NULL OR reorder_quantity > 0) AND reorder_point_quantity >= 0 AND safety_stock_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_costs",
                table: "inventory_cost_layers",
                sql: "unit_cost >= 0 AND total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_cost_layers_remaining_quantity",
                table: "inventory_cost_layers",
                sql: "remaining_quantity >= 0 AND remaining_quantity <= received_quantity");
        }
    }
}
