using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantPopupPersistenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "line_note",
                table: "shopping_cart_items",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line_note",
                table: "sales_order_lines",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "line_note",
                table: "checkout_session_lines",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_product_variants_tenant_id_product_id_id",
                table: "product_variants",
                columns: new[] { "tenant_id", "product_id", "id" });

            migrationBuilder.CreateTable(
                name: "product_recommendation_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recommended_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recommended_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recommendation_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_recommendation_links", x => x.id);
                    table.CheckConstraint("ck_product_recommendation_links_not_self", "source_product_id <> recommended_product_id");
                    table.CheckConstraint("ck_product_recommendation_links_sort_order", "sort_order >= 0");
                    table.CheckConstraint("ck_product_recommendation_links_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.CheckConstraint("ck_product_recommendation_links_type", "recommendation_type IN ('FREQUENTLY_BOUGHT_TOGETHER')");
                    table.CheckConstraint("ck_product_recommendation_links_valid_dates", "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_created_by_tenant_user_id_tenant_users",
                        column: x => x.created_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_outlet_tenant",
                        columns: x => new { x.tenant_id, x.outlet_id },
                        principalTable: "outlets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_recommended_product_tenant",
                        columns: x => new { x.tenant_id, x.recommended_product_id },
                        principalTable: "products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_recommended_variant_product_tenant",
                        columns: x => new { x.tenant_id, x.recommended_product_id, x.recommended_variant_id },
                        principalTable: "product_variants",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_sales_channel_tenant",
                        columns: x => new { x.tenant_id, x.sales_channel_id },
                        principalTable: "sales_channels",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_source_product_tenant",
                        columns: x => new { x.tenant_id, x.source_product_id },
                        principalTable: "products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_source_variant_product_tenant",
                        columns: x => new { x.tenant_id, x.source_product_id, x.source_variant_id },
                        principalTable: "product_variants",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_recommendation_links_updated_by_tenant_user_id_tenant_users",
                        column: x => x.updated_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_recommendation_links_created_by_tenant_user_id",
                table: "product_recommendation_links",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_recommendation_links_lookup",
                table: "product_recommendation_links",
                columns: new[] { "tenant_id", "source_product_id", "recommendation_type", "status", "valid_from", "valid_until" });

            migrationBuilder.CreateIndex(
                name: "IX_product_recommendation_links_tenant_id_outlet_id",
                table: "product_recommendation_links",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_recommendation_links_tenant_id_recommended_product_~",
                table: "product_recommendation_links",
                columns: new[] { "tenant_id", "recommended_product_id", "recommended_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_recommendation_links_tenant_id_sales_channel_id",
                table: "product_recommendation_links",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_recommendation_links_updated_by_tenant_user_id",
                table: "product_recommendation_links",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_recommendation_links_active_relationship",
                table: "product_recommendation_links",
                columns: new[] { "tenant_id", "source_product_id", "source_variant_id", "recommended_product_id", "recommended_variant_id", "recommendation_type", "outlet_id", "sales_channel_id" },
                unique: true,
                filter: "status = 'ACTIVE'")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_product_recommendation_links_tenant_id_id",
                table: "product_recommendation_links",
                columns: new[] { "tenant_id", "id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_recommendation_links");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_product_variants_tenant_id_product_id_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "line_note",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "line_note",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "line_note",
                table: "checkout_session_lines");

        }
    }
}
