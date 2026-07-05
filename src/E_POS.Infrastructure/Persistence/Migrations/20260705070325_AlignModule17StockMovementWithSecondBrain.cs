using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule17StockMovementWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_inventory_balance_id_inventory_balances",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_inventory_balance_id",
                table: "stock_movements");

            migrationBuilder.AlterColumn<Guid>(
                name: "fulfillment_outlet_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_created_by_tenant_user_id",
                table: "stock_movements",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_tenant_id_inventory_balance_id",
                table: "stock_movements",
                columns: new[] { "tenant_id", "inventory_balance_id" });

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_serials_tenant_id_id",
                table: "stock_movement_serials",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_references_tenant_id_id",
                table: "stock_movement_references",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_movement_cost_allocations_tenant_id_id",
                table: "stock_movement_cost_allocations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_allocations_tenant_id_id",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_created_by_tenant_user_id_tenant_users",
                table: "stock_movements",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_inventory_balance_id_inventory_balances",
                table: "stock_movements",
                columns: new[] { "tenant_id", "inventory_balance_id" },
                principalTable: "inventory_balances",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_created_by_tenant_user_id_tenant_users",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_inventory_balance_id_inventory_balances",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_created_by_tenant_user_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_tenant_id_inventory_balance_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_serials_tenant_id_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_references_tenant_id_id",
                table: "stock_movement_references");

            migrationBuilder.DropIndex(
                name: "uq_stock_movement_cost_allocations_tenant_id_id",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservation_allocations_tenant_id_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.AlterColumn<Guid>(
                name: "fulfillment_outlet_id",
                table: "inventory_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_inventory_balance_id",
                table: "stock_movements",
                column: "inventory_balance_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_inventory_balance_id_inventory_balances",
                table: "stock_movements",
                column: "inventory_balance_id",
                principalTable: "inventory_balances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
