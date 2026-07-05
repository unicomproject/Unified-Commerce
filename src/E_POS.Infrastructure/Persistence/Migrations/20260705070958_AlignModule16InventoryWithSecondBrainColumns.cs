using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule16InventoryWithSecondBrainColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_product_id_products",
                table: "product_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_product_variant_id_product_variants",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "IX_product_batches_product_id",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "IX_product_batches_product_variant_id",
                table: "product_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules");

            migrationBuilder.AlterColumn<decimal>(
                name: "reorder_quantity",
                table: "inventory_reorder_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.CreateIndex(
                name: "uq_product_inventory_settings_tenant_id_id",
                table: "product_inventory_settings",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_created_by_tenant_user_id",
                table: "product_batches",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_tenant_id_product_variant_id",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_updated_by_tenant_user_id",
                table: "product_batches",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_reorder_rules_tenant_id_id",
                table: "inventory_reorder_rules",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules",
                sql: "(reorder_quantity IS NULL OR reorder_quantity > 0) AND reorder_point_quantity >= 0 AND safety_stock_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_inventory_channel_allocations_tenant_id_id",
                table: "inventory_channel_allocations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_created_by_tenant_user_id_tenant_users",
                table: "product_batches",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_product_id_products",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_product_variant_id_product_variants",
                table: "product_batches",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_updated_by_tenant_user_id_tenant_users",
                table: "product_batches",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_created_by_tenant_user_id_tenant_users",
                table: "product_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_product_id_products",
                table: "product_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_product_variant_id_product_variants",
                table: "product_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_product_batches_updated_by_tenant_user_id_tenant_users",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "uq_product_inventory_settings_tenant_id_id",
                table: "product_inventory_settings");

            migrationBuilder.DropIndex(
                name: "IX_product_batches_created_by_tenant_user_id",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "IX_product_batches_tenant_id_product_variant_id",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "IX_product_batches_updated_by_tenant_user_id",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "uq_inventory_reorder_rules_tenant_id_id",
                table: "inventory_reorder_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules");

            migrationBuilder.DropIndex(
                name: "uq_inventory_channel_allocations_tenant_id_id",
                table: "inventory_channel_allocations");

            migrationBuilder.AlterColumn<decimal>(
                name: "reorder_quantity",
                table: "inventory_reorder_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_product_id",
                table: "product_batches",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_batches_product_variant_id",
                table: "product_batches",
                column: "product_variant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_reorder_rules_quantities",
                table: "inventory_reorder_rules",
                sql: "reorder_point_quantity >= 0 AND reorder_quantity > 0 AND safety_stock_quantity >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_product_id_products",
                table: "product_batches",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_batches_product_variant_id_product_variants",
                table: "product_batches",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
