using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnitSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {




            migrationBuilder.CreateTable(
                name: "product_unit_conversions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_level = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    conversion_to_base_factor = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_base_unit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_selling_unit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_purchase_unit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_outer_pack_unit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_unit_conversions", x => x.id);
                    table.CheckConstraint("ck_product_unit_conversions_factor", "conversion_to_base_factor > 0");
                    table.CheckConstraint("ck_product_unit_conversions_level", "unit_level IN ('BASE', 'SELLING', 'PURCHASE', 'OUTER_PACK')");
                    table.CheckConstraint("ck_product_unit_conversions_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_product_unit_conversions_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_unit_conversions_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_unit_conversions_uom_id_unit_of_measures",
                        column: x => x.uom_id,
                        principalTable: "unit_of_measures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_unit_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_model = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    base_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    selling_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchase_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outer_pack_uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    items_per_purchase_unit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    purchase_units_per_outer_pack = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    allow_decimal_quantity = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_unit_settings", x => x.id);
                    table.CheckConstraint("ck_product_unit_settings_outer_pack_factor", "purchase_units_per_outer_pack IS NULL OR purchase_units_per_outer_pack > 0");
                    table.CheckConstraint("ck_product_unit_settings_purchase_factor", "items_per_purchase_unit IS NULL OR items_per_purchase_unit > 0");
                    table.CheckConstraint("ck_product_unit_settings_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.CheckConstraint("ck_product_unit_settings_unit_model", "unit_model IN ('SINGLE_UNIT', 'MULTIPLE_UNITS')");
                    table.ForeignKey(
                        name: "fk_product_unit_settings_base_uom_id_unit_of_measures",
                        column: x => x.base_uom_id,
                        principalTable: "unit_of_measures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_unit_settings_outer_pack_uom_id_unit_of_measures",
                        column: x => x.outer_pack_uom_id,
                        principalTable: "unit_of_measures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_unit_settings_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_unit_settings_purchase_uom_id_unit_of_measures",
                        column: x => x.purchase_uom_id,
                        principalTable: "unit_of_measures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_unit_settings_selling_uom_id_unit_of_measures",
                        column: x => x.selling_uom_id,
                        principalTable: "unit_of_measures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_unit_settings_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_tenant_users_archived_by",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_tenant_users_published_by",
                table: "products");

            migrationBuilder.DropTable(
                name: "pos_order_hold_events");

            migrationBuilder.DropTable(
                name: "product_unit_conversions");

            migrationBuilder.DropTable(
                name: "product_unit_settings");

            migrationBuilder.DropIndex(
                name: "IX_products_tenant_id_archived_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_tenant_id_published_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_setup_step",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_status",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_pos_order_holds_tenant_id_id",
                table: "pos_order_holds");

            migrationBuilder.DropIndex(
                name: "uq_pos_order_holds_tenant_id_idempotency_key",
                table: "pos_order_holds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_sales_channels_channel_type",
                table: "platform_sales_channels");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "archived_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "current_setup_step",
                table: "products");

            migrationBuilder.DropColumn(
                name: "draft_saved_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "published_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "products");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                table: "pos_order_holds");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_status",
                table: "products",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_sales_channels_channel_type",
                table: "platform_sales_channels",
                sql: "channel_type IN ('PHYSICAL', 'ONLINE', 'AGGREGATOR', 'B2B', 'OTHER')");
        }
    }
}
