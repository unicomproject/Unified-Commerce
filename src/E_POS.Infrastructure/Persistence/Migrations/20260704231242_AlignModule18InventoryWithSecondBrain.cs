using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule18InventoryWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_batch_id_product_batches",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_id_products",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_variant_id_product_variants",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_stock_adjustment_id_stock_adjustments",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_batch_id_product_batches",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_id_products",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_variant_id_product_variants",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_stock_transfer_id_stock_transfers",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_status_history_stock_transfer_id_stock_transfers",
                table: "stock_transfer_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_destination_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_source_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_line_serials_serial_number_id_serial_numbers",
                table: "stocktake_line_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_line_serials_stocktake_line_id_stocktake_lines",
                table: "stocktake_line_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_batch_id_product_batches",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_id_products",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_variant_id_product_variants",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_stocktake_session_id_stocktake_sessions",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_generated_stock_adjustment_id_stock_adjustments",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_inventory_location_id_inventory_locations",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_generated_stock_adjustment_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_inventory_location_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_product_batch_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_product_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_product_variant_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_stocktake_session_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_line_serials_serial_number_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_line_serials_stocktake_line_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_destination_location_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_source_location_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_status_history_stock_transfer_id",
                table: "stock_transfer_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_product_batch_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_product_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_product_variant_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_stock_transfer_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_product_batch_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_product_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_product_variant_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_stock_adjustment_id",
                table: "stock_adjustment_lines");

            migrationBuilder.AlterColumn<string>(
                name: "direction",
                table: "stock_adjustment_reasons",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_stocktake_sessions_tenant_id_id",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_stocktake_lines_tenant_id_id",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_stock_transfers_tenant_id_id",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_stock_adjustments_tenant_id_id",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_created_by_tenant_user_id",
                table: "stocktake_sessions",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_tenant_id_generated_stock_adjustment_id",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "generated_stock_adjustment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_tenant_id_inventory_location_id",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "inventory_location_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_updated_by_tenant_user_id",
                table: "stocktake_sessions",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_sessions_tenant_id_id",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_tenant_id_product_batch_id",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "product_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_tenant_id_product_id",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_tenant_id_product_variant_id",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_lines_tenant_id_id",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_line_serials_tenant_id_serial_number_id",
                table: "stocktake_line_serials",
                columns: new[] { "tenant_id", "serial_number_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_created_by_tenant_user_id",
                table: "stock_transfers",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_tenant_id_destination_location_id",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "destination_location_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_tenant_id_source_location_id",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "source_location_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_updated_by_tenant_user_id",
                table: "stock_transfers",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfers_tenant_id_id",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_tenant_id_product_batch_id",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "product_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_tenant_id_product_id",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_tenant_id_product_variant_id",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_lines_tenant_id_id",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustments_tenant_id_id",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_reasons_created_by_tenant_user_id",
                table: "stock_adjustment_reasons",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_reasons_updated_by_tenant_user_id",
                table: "stock_adjustment_reasons",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_stock_adjustment_reasons_tenant_id_id",
                table: "stock_adjustment_reasons",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_adjustment_reasons_status",
                table: "stock_adjustment_reasons",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_tenant_id_product_batch_id",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "product_batch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_tenant_id_product_id",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_tenant_id_product_variant_id",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_batch_id_product_batches",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "product_batch_id" },
                principalTable: "product_batches",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_id_products",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_variant_id_product_variants",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_stock_adjustment_id_stock_adjustments",
                table: "stock_adjustment_lines",
                columns: new[] { "tenant_id", "stock_adjustment_id" },
                principalTable: "stock_adjustments",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_reasons_created_by_tenant_user_id_tenant_users",
                table: "stock_adjustment_reasons",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_reasons_updated_by_tenant_user_id_tenant_users",
                table: "stock_adjustment_reasons",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_product_batch_id_product_batches",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "product_batch_id" },
                principalTable: "product_batches",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_product_id_products",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_product_variant_id_product_variants",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_lines_stock_transfer_id_stock_transfers",
                table: "stock_transfer_lines",
                columns: new[] { "tenant_id", "stock_transfer_id" },
                principalTable: "stock_transfers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_status_history_stock_transfer_id_stock_transfers",
                table: "stock_transfer_status_history",
                columns: new[] { "tenant_id", "stock_transfer_id" },
                principalTable: "stock_transfers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_created_by_tenant_user_id_tenant_users",
                table: "stock_transfers",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_destination_location_id_inventory_locations",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "destination_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_source_location_id_inventory_locations",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "source_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_updated_by_tenant_user_id_tenant_users",
                table: "stock_transfers",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_line_serials_serial_number_id_serial_numbers",
                table: "stocktake_line_serials",
                columns: new[] { "tenant_id", "serial_number_id" },
                principalTable: "serial_numbers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_line_serials_stocktake_line_id_stocktake_lines",
                table: "stocktake_line_serials",
                columns: new[] { "tenant_id", "stocktake_line_id" },
                principalTable: "stocktake_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_product_batch_id_product_batches",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "product_batch_id" },
                principalTable: "product_batches",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_product_id_products",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_product_variant_id_product_variants",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_stocktake_session_id_stocktake_sessions",
                table: "stocktake_lines",
                columns: new[] { "tenant_id", "stocktake_session_id" },
                principalTable: "stocktake_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_created_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_generated_stock_adjustment_id_stock_adjustments",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "generated_stock_adjustment_id" },
                principalTable: "stock_adjustments",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_inventory_location_id_inventory_locations",
                table: "stocktake_sessions",
                columns: new[] { "tenant_id", "inventory_location_id" },
                principalTable: "inventory_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_updated_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_batch_id_product_batches",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_id_products",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_product_variant_id_product_variants",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_lines_stock_adjustment_id_stock_adjustments",
                table: "stock_adjustment_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_reasons_created_by_tenant_user_id_tenant_users",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_adjustment_reasons_updated_by_tenant_user_id_tenant_users",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_batch_id_product_batches",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_id_products",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_product_variant_id_product_variants",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_lines_stock_transfer_id_stock_transfers",
                table: "stock_transfer_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfer_status_history_stock_transfer_id_stock_transfers",
                table: "stock_transfer_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_created_by_tenant_user_id_tenant_users",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_destination_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_source_location_id_inventory_locations",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_updated_by_tenant_user_id_tenant_users",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_line_serials_serial_number_id_serial_numbers",
                table: "stocktake_line_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_line_serials_stocktake_line_id_stocktake_lines",
                table: "stocktake_line_serials");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_batch_id_product_batches",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_id_products",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_product_variant_id_product_variants",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_stocktake_session_id_stocktake_sessions",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_created_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_generated_stock_adjustment_id_stock_adjustments",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_inventory_location_id_inventory_locations",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_updated_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_stocktake_sessions_tenant_id_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_created_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_tenant_id_generated_stock_adjustment_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_tenant_id_inventory_location_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_updated_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_sessions_tenant_id_id",
                table: "stocktake_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_stocktake_lines_tenant_id_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_tenant_id_product_batch_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_tenant_id_product_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_tenant_id_product_variant_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_lines_tenant_id_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_line_serials_tenant_id_serial_number_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_stock_transfers_tenant_id_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_created_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_tenant_id_destination_location_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_tenant_id_source_location_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_updated_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfers_tenant_id_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_tenant_id_product_batch_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_tenant_id_product_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfer_lines_tenant_id_product_variant_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_lines_tenant_id_id",
                table: "stock_transfer_lines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_stock_adjustments_tenant_id_id",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "uq_stock_adjustments_tenant_id_id",
                table: "stock_adjustments");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_reasons_created_by_tenant_user_id",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_reasons_updated_by_tenant_user_id",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropIndex(
                name: "uq_stock_adjustment_reasons_tenant_id_id",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_adjustment_reasons_status",
                table: "stock_adjustment_reasons");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_tenant_id_product_batch_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_tenant_id_product_id",
                table: "stock_adjustment_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_adjustment_lines_tenant_id_product_variant_id",
                table: "stock_adjustment_lines");

            migrationBuilder.AlterColumn<string>(
                name: "direction",
                table: "stock_adjustment_reasons",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_generated_stock_adjustment_id",
                table: "stocktake_sessions",
                column: "generated_stock_adjustment_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_inventory_location_id",
                table: "stocktake_sessions",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_product_batch_id",
                table: "stocktake_lines",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_product_id",
                table: "stocktake_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_product_variant_id",
                table: "stocktake_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_stocktake_session_id",
                table: "stocktake_lines",
                column: "stocktake_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_line_serials_serial_number_id",
                table: "stocktake_line_serials",
                column: "serial_number_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_line_serials_stocktake_line_id",
                table: "stocktake_line_serials",
                column: "stocktake_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_destination_location_id",
                table: "stock_transfers",
                column: "destination_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_source_location_id",
                table: "stock_transfers",
                column: "source_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_status_history_stock_transfer_id",
                table: "stock_transfer_status_history",
                column: "stock_transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_product_batch_id",
                table: "stock_transfer_lines",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_product_id",
                table: "stock_transfer_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_product_variant_id",
                table: "stock_transfer_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_stock_transfer_id",
                table: "stock_transfer_lines",
                column: "stock_transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_product_batch_id",
                table: "stock_adjustment_lines",
                column: "product_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_product_id",
                table: "stock_adjustment_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_product_variant_id",
                table: "stock_adjustment_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_lines_stock_adjustment_id",
                table: "stock_adjustment_lines",
                column: "stock_adjustment_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_batch_id_product_batches",
                table: "stock_adjustment_lines",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_adjustment_lines_product_id_products",
                table: "stock_adjustment_lines",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_stock_adjustment_lines_stock_adjustment_id_stock_adjustments",
                table: "stock_adjustment_lines",
                column: "stock_adjustment_id",
                principalTable: "stock_adjustments",
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
                name: "fk_stock_transfer_lines_product_id_products",
                table: "stock_transfer_lines",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_stock_transfer_lines_stock_transfer_id_stock_transfers",
                table: "stock_transfer_lines",
                column: "stock_transfer_id",
                principalTable: "stock_transfers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfer_status_history_stock_transfer_id_stock_transfers",
                table: "stock_transfer_status_history",
                column: "stock_transfer_id",
                principalTable: "stock_transfers",
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
                name: "fk_stocktake_line_serials_serial_number_id_serial_numbers",
                table: "stocktake_line_serials",
                column: "serial_number_id",
                principalTable: "serial_numbers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_line_serials_stocktake_line_id_stocktake_lines",
                table: "stocktake_line_serials",
                column: "stocktake_line_id",
                principalTable: "stocktake_lines",
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
                name: "fk_stocktake_lines_product_id_products",
                table: "stocktake_lines",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_stocktake_lines_stocktake_session_id_stocktake_sessions",
                table: "stocktake_lines",
                column: "stocktake_session_id",
                principalTable: "stocktake_sessions",
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
                name: "fk_stocktake_sessions_inventory_location_id_inventory_locations",
                table: "stocktake_sessions",
                column: "inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
