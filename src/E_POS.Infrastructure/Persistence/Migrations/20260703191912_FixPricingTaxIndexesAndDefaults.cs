using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPricingTaxIndexesAndDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE tax_rates SET status = 'ACTIVE' WHERE status = '';
                UPDATE tax_jurisdictions SET status = 'ACTIVE' WHERE status = '';
                UPDATE tax_classes SET status = 'ACTIVE' WHERE status = '';
                UPDATE tax_class_rates SET status = 'ACTIVE' WHERE status = '';
                UPDATE product_tax_assignments SET status = 'ACTIVE' WHERE status = '';
                UPDATE price_list_items SET status = 'ACTIVE' WHERE status = '';
                UPDATE price_list_outlets SET status = 'ACTIVE' WHERE status = '';
                UPDATE price_list_channels SET status = 'ACTIVE' WHERE status = '';

                UPDATE tax_rates tr
                SET tenant_id = tj.tenant_id
                FROM tax_jurisdictions tj
                WHERE tr.tax_jurisdiction_id = tj.id
                  AND tr.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE tax_class_rates tcr
                SET tenant_id = tc.tenant_id
                FROM tax_classes tc
                WHERE tcr.tax_class_id = tc.id
                  AND tcr.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE product_tax_assignments pta
                SET tenant_id = p.tenant_id
                FROM products p
                WHERE pta.product_id = p.id
                  AND pta.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_list_outlets plo
                SET tenant_id = pl.tenant_id
                FROM price_lists pl
                WHERE plo.price_list_id = pl.id
                  AND plo.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_list_channels plc
                SET tenant_id = pl.tenant_id
                FROM price_lists pl
                WHERE plc.price_list_id = pl.id
                  AND plc.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_list_items pli
                SET tenant_id = pl.tenant_id
                FROM price_lists pl
                WHERE pli.price_list_id = pl.id
                  AND pli.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_lists pl
                SET currency_code = t.currency_code
                FROM tenants t
                WHERE pl.tenant_id = t.id
                  AND pl.currency_code = '';

                UPDATE price_lists SET price_list_type = 'STANDARD' WHERE price_list_type = '';
                UPDATE tax_jurisdictions SET jurisdiction_type = 'COUNTRY' WHERE jurisdiction_type = '';
                UPDATE tax_jurisdictions SET country_code = 'LK' WHERE country_code = '';

                ALTER TABLE tax_rates ALTER COLUMN status DROP DEFAULT;
                ALTER TABLE tax_rates ALTER COLUMN tenant_id DROP DEFAULT;
                ALTER TABLE tax_rates ALTER COLUMN is_compound DROP DEFAULT;
                ALTER TABLE tax_jurisdictions ALTER COLUMN status DROP DEFAULT;
                ALTER TABLE tax_jurisdictions ALTER COLUMN country_code DROP DEFAULT;
                ALTER TABLE tax_jurisdictions ALTER COLUMN jurisdiction_type DROP DEFAULT;
                ALTER TABLE tax_classes ALTER COLUMN status DROP DEFAULT;
                ALTER TABLE tax_classes ALTER COLUMN is_default_tax_class DROP DEFAULT;
                ALTER TABLE tax_class_rates ALTER COLUMN status DROP DEFAULT;
                ALTER TABLE tax_class_rates ALTER COLUMN tenant_id DROP DEFAULT;
                ALTER TABLE tax_class_rates ALTER COLUMN sort_order DROP DEFAULT;
                ALTER TABLE product_tax_assignments ALTER COLUMN status DROP DEFAULT;
                ALTER TABLE product_tax_assignments ALTER COLUMN tenant_id DROP DEFAULT;
                ALTER TABLE price_lists ALTER COLUMN currency_code DROP DEFAULT;
                ALTER TABLE price_lists ALTER COLUMN price_list_type DROP DEFAULT;
                ALTER TABLE price_lists ALTER COLUMN is_default_price_list DROP DEFAULT;
                ALTER TABLE price_lists ALTER COLUMN price_includes_tax DROP DEFAULT;
                ALTER TABLE price_lists ALTER COLUMN priority DROP DEFAULT;
                ALTER TABLE price_list_outlets ALTER COLUMN tenant_id DROP DEFAULT;
                ALTER TABLE price_list_outlets ALTER COLUMN outlet_id DROP DEFAULT;
                ALTER TABLE price_list_items ALTER COLUMN status DROP DEFAULT;
                ALTER TABLE price_list_items ALTER COLUMN tenant_id DROP DEFAULT;
                ALTER TABLE price_list_items ALTER COLUMN min_quantity DROP DEFAULT;
                ALTER TABLE price_list_channels ALTER COLUMN tenant_id DROP DEFAULT;
            """);
            migrationBuilder.DropIndex(
                name: "uq_product_tax_assignments_tenant_product_variant_tax_class",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_product_scope",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_variant_scope",
                table: "price_list_items");

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_active_product",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "product_id" },
                unique: true,
                filter: "product_variant_id IS NULL AND status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_active_variant",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL AND status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_product_scope_no_uom",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NULL AND uom_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_product_scope_with_uom",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_id", "uom_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NULL AND uom_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_variant_scope_no_uom",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_variant_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NOT NULL AND uom_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_variant_scope_with_uom",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_variant_id", "uom_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NOT NULL AND uom_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_product_tax_assignments_active_product",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "uq_product_tax_assignments_active_variant",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_product_scope_no_uom",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_product_scope_with_uom",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_variant_scope_no_uom",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_variant_scope_with_uom",
                table: "price_list_items");

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_tenant_product_variant_tax_class",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "product_id", "product_variant_id", "tax_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_product_scope",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_id", "uom_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_variant_scope",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_variant_id", "uom_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");
        }
    }
}

