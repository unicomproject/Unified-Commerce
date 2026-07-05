using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule14PricingWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_tax_class_rates_tenant_id_id",
                table: "tax_class_rates",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_tenant_id_id",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_list_outlets_tenant_id_id",
                table: "price_list_outlets",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_tenant_id_id",
                table: "price_list_items",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_list_channels_tenant_id_id",
                table: "price_list_channels",
                columns: new[] { "tenant_id", "id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tax_class_rates_tenant_id_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "uq_product_tax_assignments_tenant_id_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "uq_price_list_outlets_tenant_id_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_tenant_id_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_channels_tenant_id_id",
                table: "price_list_channels");
        }
    }
}
