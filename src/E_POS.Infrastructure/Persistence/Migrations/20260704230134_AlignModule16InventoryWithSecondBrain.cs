using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule16InventoryWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_inventory_location_id_inventory_locations",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_batch_id_product_batches",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_id_products",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_variant_id_product_variants",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_inventory_location_id_inventory_locations",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_product_id_products",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_product_variant_id_product_variants",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_sales_channel_id_sales_channels",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_inventory_balance_id_inventory_balances",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_source_stock_movement_id_stock_movements",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_outlet_id_outlets",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_parent_inventory_location_id_inventory_locations",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_inventory_location_id_inventory_locations",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_product_id_products",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_product_variant_id_product_variants",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_product_id_products",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_product_variant_id_product_variants",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_current_inventory_balance_id_inventory_balances",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_batch_id_product_batches",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_id_products",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_variant_id_product_variants",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_current_inventory_balance_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_product_batch_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_product_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_product_variant_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_product_inventory_settings_product_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "IX_product_inventory_settings_product_variant_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_inventory_location_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_product_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_product_variant_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_locations_outlet_id",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_locations_parent_inventory_location_id",
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
                name: "IX_inventory_channel_allocations_sales_channel_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_inventory_location_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_product_batch_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_product_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_product_variant_id",
                table: "inventory_balances");

            migrationBuilder.AlterColumn<string>(
                name: "serial_status",
                table: "serial_numbers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "inventory_cost_layers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_stock_movements_tenant_id_id",
                table: "stock_movements",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_inventory_locations_tenant_id_id",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_inventory_balances_tenant_id_id",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "uq_stock_movements_tenant_id_id",
                table: "stock_movements",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_created_by_tenant_user_id",
                table: "serial_numbers",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_tenant_id_current_inventory_balance_id",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "current_inventory_balance_id" });

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_tenant_id_product_batch_id",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "product_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_tenant_id_product_variant_id",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_updated_by_tenant_user_id",
                table: "serial_numbers",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_created_by_tenant_user_id",
                table: "product_inventory_settings",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_updated_by_tenant_user_id",
                table: "product_inventory_settings",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_inventory_settings_status",
                table: "product_inventory_settings",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_created_by_tenant_user_id",
                table: "inventory_reorder_rules",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_tenant_id_product_id",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_tenant_id_product_variant_id",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_updated_by_tenant_user_id",
                table: "inventory_reorder_rules",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules",
                sql: "reorder_point_quantity >= 0 AND reorder_quantity > 0 AND safety_stock_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_status",
                table: "inventory_reorder_rules",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_created_by_tenant_user_id",
                table: "inventory_locations",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_tenant_id_parent_inventory_location_id",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "parent_inventory_location_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_updated_by_tenant_user_id",
                table: "inventory_locations",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_locations_status",
                table: "inventory_locations",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_tenant_id_inventory_balance_id",
                table: "inventory_cost_layers",
                columns: new[] { "tenant_id", "inventory_balance_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_cost_layers_tenant_id_source_stock_movement_id",
                table: "inventory_cost_layers",
                columns: new[] { "tenant_id", "source_stock_movement_id" });

            migrationBuilder.CreateIndex(
                name: "uq_inventory_cost_layers_tenant_id_id",
                table: "inventory_cost_layers",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_created_by_tenant_user_id",
                table: "inventory_channel_allocations",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_tenant_id_product_id",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_tenant_id_product_variant_id",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_tenant_id_sales_channel_id",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_channel_allocations_updated_by_tenant_user_id",
                table: "inventory_channel_allocations",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations",
                sql: "allocation_limit_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_channel_allocations_status",
                table: "inventory_channel_allocations",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_tenant_id_product_batch_id",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "product_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_tenant_id_product_id",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_tenant_id_product_variant_id",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_inventory_balances_tenant_id_id",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_balances_on_hand_quantity",
                table: "inventory_balances",
                sql: "on_hand_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_balances_row_version",
                table: "inventory_balances",
                sql: "row_version >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_inventory_location_id_inventory_locations",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "inventory_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_product_batch_id_product_batches",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "product_batch_id" },
                principalTable: "product_batches",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_product_id_products",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_product_variant_id_product_variants",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_created_by_tenant_user_id_tenant_users",
                table: "inventory_channel_allocations",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_inventory_location_id_inventory_locations",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "inventory_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_product_id_products",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_product_variant_id_product_variants",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_sales_channel_id_sales_channels",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_channel_allocations_updated_by_tenant_user_id_tenant_users",
                table: "inventory_channel_allocations",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_cost_layers_inventory_balance_id_inventory_balances",
                table: "inventory_cost_layers",
                columns: new[] { "tenant_id", "inventory_balance_id" },
                principalTable: "inventory_balances",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_cost_layers_source_stock_movement_id_stock_movements",
                table: "inventory_cost_layers",
                columns: new[] { "tenant_id", "source_stock_movement_id" },
                principalTable: "stock_movements",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_locations_created_by_tenant_user_id_tenant_users",
                table: "inventory_locations",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_locations_outlet_id_outlets",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_locations_parent_inventory_location_id_inventory_locations",
                table: "inventory_locations",
                columns: new[] { "tenant_id", "parent_inventory_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_locations_updated_by_tenant_user_id_tenant_users",
                table: "inventory_locations",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_created_by_tenant_user_id_tenant_users",
                table: "inventory_reorder_rules",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_inventory_location_id_inventory_locations",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "inventory_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_product_id_products",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_product_variant_id_product_variants",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_updated_by_tenant_user_id_tenant_users",
                table: "inventory_reorder_rules",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_created_by_tenant_user_id_tenant_users",
                table: "product_inventory_settings",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_product_id_products",
                table: "product_inventory_settings",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_product_variant_id_product_variants",
                table: "product_inventory_settings",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_updated_by_tenant_user_id_tenant_users",
                table: "product_inventory_settings",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_created_by_tenant_user_id_tenant_users",
                table: "serial_numbers",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_current_inventory_balance_id_inventory_balances",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "current_inventory_balance_id" },
                principalTable: "inventory_balances",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_product_batch_id_product_batches",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "product_batch_id" },
                principalTable: "product_batches",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_product_id_products",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_product_variant_id_product_variants",
                table: "serial_numbers",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_updated_by_tenant_user_id_tenant_users",
                table: "serial_numbers",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_inventory_location_id_inventory_locations",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_batch_id_product_batches",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_id_products",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_balances_product_variant_id_product_variants",
                table: "inventory_balances");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_created_by_tenant_user_id_tenant_users",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_inventory_location_id_inventory_locations",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_product_id_products",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_product_variant_id_product_variants",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_sales_channel_id_sales_channels",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_channel_allocations_updated_by_tenant_user_id_tenant_users",
                table: "inventory_channel_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_inventory_balance_id_inventory_balances",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_cost_layers_source_stock_movement_id_stock_movements",
                table: "inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_created_by_tenant_user_id_tenant_users",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_outlet_id_outlets",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_parent_inventory_location_id_inventory_locations",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_locations_updated_by_tenant_user_id_tenant_users",
                table: "inventory_locations");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_created_by_tenant_user_id_tenant_users",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_inventory_location_id_inventory_locations",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_product_id_products",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_product_variant_id_product_variants",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_reorder_rules_updated_by_tenant_user_id_tenant_users",
                table: "inventory_reorder_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_created_by_tenant_user_id_tenant_users",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_product_id_products",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_product_variant_id_product_variants",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_product_inventory_settings_updated_by_tenant_user_id_tenant_users",
                table: "product_inventory_settings");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_created_by_tenant_user_id_tenant_users",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_current_inventory_balance_id_inventory_balances",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_batch_id_product_batches",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_id_products",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_product_variant_id_product_variants",
                table: "serial_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_serial_numbers_updated_by_tenant_user_id_tenant_users",
                table: "serial_numbers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_stock_movements_tenant_id_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "uq_stock_movements_tenant_id_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_created_by_tenant_user_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_tenant_id_current_inventory_balance_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_tenant_id_product_batch_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_tenant_id_product_variant_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_serial_numbers_updated_by_tenant_user_id",
                table: "serial_numbers");

            migrationBuilder.DropIndex(
                name: "IX_product_inventory_settings_created_by_tenant_user_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "IX_product_inventory_settings_updated_by_tenant_user_id",
                table: "product_inventory_settings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_inventory_settings_status",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_created_by_tenant_user_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_tenant_id_product_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_tenant_id_product_variant_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "IX_inventory_reorder_rules_updated_by_tenant_user_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_status",
                table: "inventory_reorder_rules");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_inventory_locations_tenant_id_id",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_locations_created_by_tenant_user_id",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_locations_tenant_id_parent_inventory_location_id",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_locations_updated_by_tenant_user_id",
                table: "inventory_locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_locations_status",
                table: "inventory_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_cost_layers_tenant_id_inventory_balance_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "IX_inventory_cost_layers_tenant_id_source_stock_movement_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "uq_inventory_cost_layers_tenant_id_id",
                table: "inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_created_by_tenant_user_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_tenant_id_product_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_tenant_id_product_variant_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_tenant_id_sales_channel_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_channel_allocations_updated_by_tenant_user_id",
                table: "inventory_channel_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_channel_allocations_status",
                table: "inventory_channel_allocations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_inventory_balances_tenant_id_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_tenant_id_product_batch_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_tenant_id_product_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_tenant_id_product_variant_id",
                table: "inventory_balances");

            migrationBuilder.DropIndex(
                name: "uq_inventory_balances_tenant_id_id",
                table: "inventory_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_balances_on_hand_quantity",
                table: "inventory_balances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_balances_row_version",
                table: "inventory_balances");

            migrationBuilder.AlterColumn<string>(
                name: "serial_status",
                table: "serial_numbers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "inventory_cost_layers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_current_inventory_balance_id",
                table: "serial_numbers",
                column: "current_inventory_balance_id");

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_product_batch_id",
                table: "serial_numbers",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_product_id",
                table: "serial_numbers",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_serial_numbers_product_variant_id",
                table: "serial_numbers",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_product_id",
                table: "product_inventory_settings",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_settings_product_variant_id",
                table: "product_inventory_settings",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_inventory_location_id",
                table: "inventory_reorder_rules",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_product_id",
                table: "inventory_reorder_rules",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reorder_rules_product_variant_id",
                table: "inventory_reorder_rules",
                column: "product_variant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules",
                sql: "reorder_point_quantity > 0 AND reorder_quantity > 0 AND safety_stock_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_outlet_id",
                table: "inventory_locations",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_locations_parent_inventory_location_id",
                table: "inventory_locations",
                column: "parent_inventory_location_id");

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
                name: "IX_inventory_channel_allocations_sales_channel_id",
                table: "inventory_channel_allocations",
                column: "sales_channel_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_channel_allocations_allocation_limit_quantity",
                table: "inventory_channel_allocations",
                sql: "allocation_limit_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_inventory_location_id",
                table: "inventory_balances",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_product_batch_id",
                table: "inventory_balances",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_product_id",
                table: "inventory_balances",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_product_variant_id",
                table: "inventory_balances",
                column: "product_variant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_balances_inventory_location_id_inventory_locations",
                table: "inventory_balances",
                column: "inventory_location_id",
                principalTable: "inventory_locations",
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
                name: "fk_inventory_balances_product_id_products",
                table: "inventory_balances",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_inventory_channel_allocations_inventory_location_id_inventory_locations",
                table: "inventory_channel_allocations",
                column: "inventory_location_id",
                principalTable: "inventory_locations",
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
                name: "fk_inventory_channel_allocations_sales_channel_id_sales_channels",
                table: "inventory_channel_allocations",
                column: "sales_channel_id",
                principalTable: "sales_channels",
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
                name: "fk_inventory_locations_outlet_id_outlets",
                table: "inventory_locations",
                column: "outlet_id",
                principalTable: "outlets",
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
                name: "fk_inventory_reorder_rules_inventory_location_id_inventory_locations",
                table: "inventory_reorder_rules",
                column: "inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_reorder_rules_product_id_products",
                table: "inventory_reorder_rules",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_product_inventory_settings_product_id_products",
                table: "product_inventory_settings",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_inventory_settings_product_variant_id_product_variants",
                table: "product_inventory_settings",
                column: "product_variant_id",
                principalTable: "product_variants",
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
                name: "fk_serial_numbers_product_id_products",
                table: "serial_numbers",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_serial_numbers_product_variant_id_product_variants",
                table: "serial_numbers",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
