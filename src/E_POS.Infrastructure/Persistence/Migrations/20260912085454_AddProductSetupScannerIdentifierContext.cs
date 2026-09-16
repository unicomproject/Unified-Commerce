using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSetupScannerIdentifierContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "identifier_standard",
                table: "product_barcodes",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_setup_scan_context",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acquisition_mode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    candidate_identifier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    identifier_standard = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    symbology_hint = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    no_barcode_reason = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    external_lookup_status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    external_source_reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    normalized_prefill_json = table.Column<string>(type: "jsonb", nullable: true),
                    generated_sku_candidate = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_setup_scan_context", x => x.id);
                    table.CheckConstraint("ck_product_setup_scan_context_acquisition_mode", "acquisition_mode IN ('SCAN', 'MANUAL', 'NO_BARCODE', 'LEGACY')");
                    table.CheckConstraint("ck_product_setup_scan_context_no_barcode_reason", "no_barcode_reason IS NULL OR no_barcode_reason IN ('OWN_MADE', 'SERVICE_FEE', 'UNLABELLED')");
                    table.CheckConstraint("ck_product_setup_scan_context_row_version", "row_version >= 1");
                    table.ForeignKey(
                        name: "fk_product_setup_scan_context_created_by",
                        column: x => x.created_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_setup_scan_context_product_id_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_setup_scan_context_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_setup_scan_context_updated_by",
                        column: x => x.updated_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_barcodes_identifier_standard",
                table: "product_barcodes",
                sql: "identifier_standard IS NULL OR identifier_standard IN ('GTIN8', 'GTIN12', 'GTIN13', 'GTIN14', 'OTHER')");

            migrationBuilder.CreateIndex(
                name: "IX_product_setup_scan_context_created_by_tenant_user_id",
                table: "product_setup_scan_context",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_setup_scan_context_updated_by_tenant_user_id",
                table: "product_setup_scan_context",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_setup_scan_context_tenant_id_id",
                table: "product_setup_scan_context",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_setup_scan_context_tenant_id_product_id",
                table: "product_setup_scan_context",
                columns: new[] { "tenant_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_setup_scan_context");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_barcodes_identifier_standard",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "identifier_standard",
                table: "product_barcodes");
        }
    }
}
