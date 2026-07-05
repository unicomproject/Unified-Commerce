using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule17InventoryWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_serial_number_id_serial_numbers",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_product_id_products",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_product_variant_id_product_variants",
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
                name: "fk_stock_movement_cost_allocations_inventory_cost_layer_id_inventory_cost_layers",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_cost_allocations_stock_movement_id_stock_movements",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_references_stock_movement_id_stock_movements",
                table: "stock_movement_references");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_serials_serial_number_id_serial_numbers",
                table: "stock_movement_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_serials_stock_movement_id_stock_movements",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_serials_serial_number_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_serials_stock_movement_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_references_stock_movement_id",
                table: "stock_movement_references");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_cost_allocations_inventory_cost_layer_id",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_cost_allocations_stock_movement_id",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_customer_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_fulfillment_outlet_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_sales_channel_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_inventory_reservation_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_product_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_product_variant_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_allocations_inventory_balance_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_allocations_inventory_reservation_lin~",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_allocations_serial_number_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_serial_numbers_tenant_id_id",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_inventory_reservations_tenant_id_id",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_inventory_reservation_lines_tenant_id_id",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_inventory_cost_layers_tenant_id_id",
                table: "inventory_cost_layers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customers_tenant_id_id",
                table: "customers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_serials_tenant_id_serial_number_id",
                table: "stock_movement_serials",
                columns: new[] { "tenant_id", "serial_number_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_cost_allocations_tenant_id_inventory_cost_la~",
                table: "stock_movement_cost_allocations",
                columns: new[] { "tenant_id", "inventory_cost_layer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_created_by_tenant_user_id",
                table: "inventory_reservations",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_tenant_id_customer_id",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_tenant_id_fulfillment_outlet_id",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "fulfillment_outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_tenant_id_sales_channel_id",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_updated_by_tenant_user_id",
                table: "inventory_reservations",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservations_tenant_id_id",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_tenant_id_product_id",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_tenant_id_product_variant_id",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reservation_lines_tenant_id_id",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_tenant_id_inventory_balan~",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "inventory_balance_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_tenant_id_serial_number_id",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "serial_number_id" });

            migrationBuilder.CreateIndex(
                name: "uq_customers_tenant_id_id",
                table: "customers",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "inventory_balance_id" },
                principalTable: "inventory_balances",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "inventory_reservation_line_id" },
                principalTable: "inventory_reservation_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_serial_number_id_serial_numbers",
                table: "inventory_reservation_allocations",
                columns: new[] { "tenant_id", "serial_number_id" },
                principalTable: "serial_numbers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "inventory_reservation_id" },
                principalTable: "inventory_reservations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_lines_product_id_products",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_lines_product_variant_id_product_variants",
                table: "inventory_reservation_lines",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_created_by_tenant_user_id_tenant_users",
                table: "inventory_reservations",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_customer_id_customers",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "customer_id" },
                principalTable: "customers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_fulfillment_outlet_id_outlets",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "fulfillment_outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_sales_channel_id_sales_channels",
                table: "inventory_reservations",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservations_updated_by_tenant_user_id_tenant_users",
                table: "inventory_reservations",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_cost_allocations_inventory_cost_layer_id_inventory_cost_layers",
                table: "stock_movement_cost_allocations",
                columns: new[] { "tenant_id", "inventory_cost_layer_id" },
                principalTable: "inventory_cost_layers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_cost_allocations_stock_movement_id_stock_movements",
                table: "stock_movement_cost_allocations",
                columns: new[] { "tenant_id", "stock_movement_id" },
                principalTable: "stock_movements",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_references_stock_movement_id_stock_movements",
                table: "stock_movement_references",
                columns: new[] { "tenant_id", "stock_movement_id" },
                principalTable: "stock_movements",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_serials_serial_number_id_serial_numbers",
                table: "stock_movement_serials",
                columns: new[] { "tenant_id", "serial_number_id" },
                principalTable: "serial_numbers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_serials_stock_movement_id_stock_movements",
                table: "stock_movement_serials",
                columns: new[] { "tenant_id", "stock_movement_id" },
                principalTable: "stock_movements",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_allocations_serial_number_id_serial_numbers",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_product_id_products",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservation_lines_product_variant_id_product_variants",
                table: "inventory_reservation_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reservations_created_by_tenant_user_id_tenant_users",
                table: "inventory_reservations");

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
                name: "fk_inventory_reservations_updated_by_tenant_user_id_tenant_users",
                table: "inventory_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_cost_allocations_inventory_cost_layer_id_inventory_cost_layers",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_cost_allocations_stock_movement_id_stock_movements",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_references_stock_movement_id_stock_movements",
                table: "stock_movement_references");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_serials_serial_number_id_serial_numbers",
                table: "stock_movement_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movement_serials_stock_movement_id_stock_movements",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_serials_tenant_id_serial_number_id",
                table: "stock_movement_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_movement_cost_allocations_tenant_id_inventory_cost_la~",
                table: "stock_movement_cost_allocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_serial_numbers_tenant_id_id",
                table: "serial_numbers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_inventory_reservations_tenant_id_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_created_by_tenant_user_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_tenant_id_customer_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_tenant_id_fulfillment_outlet_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_tenant_id_sales_channel_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservations_updated_by_tenant_user_id",
                table: "inventory_reservations");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservations_tenant_id_id",
                table: "inventory_reservations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_inventory_reservation_lines_tenant_id_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_tenant_id_product_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_lines_tenant_id_product_variant_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reservation_lines_tenant_id_id",
                table: "inventory_reservation_lines");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_allocations_tenant_id_inventory_balan~",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reservation_allocations_tenant_id_serial_number_id",
                table: "inventory_reservation_allocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_inventory_cost_layers_tenant_id_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customers_tenant_id_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "uq_customers_tenant_id_id",
                table: "customers");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_serials_serial_number_id",
                table: "stock_movement_serials",
                column: "serial_number_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_serials_stock_movement_id",
                table: "stock_movement_serials",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_references_stock_movement_id",
                table: "stock_movement_references",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_cost_allocations_inventory_cost_layer_id",
                table: "stock_movement_cost_allocations",
                column: "inventory_cost_layer_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_cost_allocations_stock_movement_id",
                table: "stock_movement_cost_allocations",
                column: "stock_movement_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_inventory_reservation_id",
                table: "inventory_reservation_lines",
                column: "inventory_reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_product_id",
                table: "inventory_reservation_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_lines_product_variant_id",
                table: "inventory_reservation_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_inventory_balance_id",
                table: "inventory_reservation_allocations",
                column: "inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_inventory_reservation_lin~",
                table: "inventory_reservation_allocations",
                column: "inventory_reservation_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservation_allocations_serial_number_id",
                table: "inventory_reservation_allocations",
                column: "serial_number_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_balance_id_inventory_balances",
                table: "inventory_reservation_allocations",
                column: "inventory_balance_id",
                principalTable: "inventory_balances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_allocations_inventory_reservation_line_id_inventory_reservation_lines",
                table: "inventory_reservation_allocations",
                column: "inventory_reservation_line_id",
                principalTable: "inventory_reservation_lines",
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
                name: "fk_inventory_reservation_lines_inventory_reservation_id_inventory_reservations",
                table: "inventory_reservation_lines",
                column: "inventory_reservation_id",
                principalTable: "inventory_reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reservation_lines_product_id_products",
                table: "inventory_reservation_lines",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_stock_movement_cost_allocations_inventory_cost_layer_id_inventory_cost_layers",
                table: "stock_movement_cost_allocations",
                column: "inventory_cost_layer_id",
                principalTable: "inventory_cost_layers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_cost_allocations_stock_movement_id_stock_movements",
                table: "stock_movement_cost_allocations",
                column: "stock_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_references_stock_movement_id_stock_movements",
                table: "stock_movement_references",
                column: "stock_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_serials_serial_number_id_serial_numbers",
                table: "stock_movement_serials",
                column: "serial_number_id",
                principalTable: "serial_numbers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movement_serials_stock_movement_id_stock_movements",
                table: "stock_movement_serials",
                column: "stock_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
