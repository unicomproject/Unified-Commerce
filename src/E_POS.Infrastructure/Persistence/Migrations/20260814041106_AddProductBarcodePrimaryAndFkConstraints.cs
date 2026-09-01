using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBarcodePrimaryAndFkConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_uom_id",
                table: "product_barcodes",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_barcodes_tenant_id_product_id_primary",
                table: "product_barcodes",
                columns: new[] { "tenant_id", "product_id" },
                unique: true,
                filter: "product_variant_id IS NULL AND is_primary_barcode = true AND status <> 'DELETED'");

            migrationBuilder.CreateIndex(
                name: "uq_product_barcodes_tenant_id_product_variant_id_primary",
                table: "product_barcodes",
                columns: new[] { "tenant_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL AND is_primary_barcode = true AND status <> 'DELETED'");

            migrationBuilder.AddForeignKey(
                name: "fk_product_barcodes_uom_id_unit_of_measures",
                table: "product_barcodes",
                column: "uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_barcodes_uom_id_unit_of_measures",
                table: "product_barcodes");

            migrationBuilder.DropIndex(
                name: "IX_product_barcodes_uom_id",
                table: "product_barcodes");

            migrationBuilder.DropIndex(
                name: "uq_product_barcodes_tenant_id_product_id_primary",
                table: "product_barcodes");

            migrationBuilder.DropIndex(
                name: "uq_product_barcodes_tenant_id_product_variant_id_primary",
                table: "product_barcodes");
        }
    }
}
