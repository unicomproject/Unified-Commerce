using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddECommerceFrontendEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_wishlists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_wishlists", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_wishlists_customer_id_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_wishlists_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_rating_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    total_reviews = table.Column<int>(type: "integer", nullable: false),
                    five_star_count = table.Column<int>(type: "integer", nullable: false),
                    four_star_count = table.Column<int>(type: "integer", nullable: false),
                    three_star_count = table.Column<int>(type: "integer", nullable: false),
                    two_star_count = table.Column<int>(type: "integer", nullable: false),
                    one_star_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_rating_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_rating_summaries_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_rating_summaries_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating_value = table.Column<int>(type: "integer", nullable: false),
                    review_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    review_text = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_reviews_customer_id_customers",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_reviews_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_reviews_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "storefront_banners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_channel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    banner_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtitle = table.Column<string>(type: "text", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    action_text = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storefront_banners", x => x.id);
                    table.ForeignKey(
                        name: "fk_storefront_banners_sales_channel_id_sales_channels",
                        column: x => x.sales_channel_id,
                        principalTable: "sales_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_storefront_banners_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_wishlist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_wishlist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_wishlist_items_customer_wishlists_wishlist_id",
                        column: x => x.wishlist_id,
                        principalTable: "customer_wishlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_wishlist_items_product_id_products",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_wishlist_items_product_variant_id_product_variants",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_wishlist_items_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_wishlist_items_product_id",
                table: "customer_wishlist_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_wishlist_items_product_variant_id",
                table: "customer_wishlist_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_wishlist_items_tenant_id",
                table: "customer_wishlist_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_wishlist_items_unique_product",
                table: "customer_wishlist_items",
                columns: new[] { "wishlist_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_wishlist_items_wishlist_id",
                table: "customer_wishlist_items",
                column: "wishlist_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_wishlists_customer_id",
                table: "customer_wishlists",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_wishlists_tenant_id",
                table: "customer_wishlists",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_rating_summaries_product_id",
                table: "product_rating_summaries",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_rating_summaries_tenant_id",
                table: "product_rating_summaries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_customer_id",
                table: "product_reviews",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_product_id",
                table: "product_reviews",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_tenant_id",
                table: "product_reviews",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_storefront_banners_sales_channel_id",
                table: "storefront_banners",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_storefront_banners_tenant_id",
                table: "storefront_banners",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_wishlist_items");

            migrationBuilder.DropTable(
                name: "product_rating_summaries");

            migrationBuilder.DropTable(
                name: "product_reviews");

            migrationBuilder.DropTable(
                name: "storefront_banners");

            migrationBuilder.DropTable(
                name: "customer_wishlists");
        }
    }
}
