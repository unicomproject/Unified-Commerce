using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260723152932_AddMediaAssetsPhase1")]
    public partial class AddMediaAssetsPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "logo_media_asset_id",
                table: "tenant_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_media_asset_id",
                table: "storefront_banners",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_media_asset_id",
                table: "product_option_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "media_asset_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_media_asset_id",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "logo_media_asset_id",
                table: "brands",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    public_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    mime_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    file_extension = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width_px = table.Column<int>(type: "integer", nullable: true),
                    height_px = table.Column<int>(type: "integer", nullable: true),
                    checksum_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    asset_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    asset_purpose = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.id);
                    table.UniqueConstraint("ak_media_assets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_media_assets_asset_type", "asset_type IN ('IMAGE')");
                    table.CheckConstraint("ck_media_assets_file_size_bytes", "file_size_bytes > 0");
                    table.CheckConstraint("ck_media_assets_height_px", "height_px IS NULL OR height_px > 0");
                    table.CheckConstraint("ck_media_assets_status", "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
                    table.CheckConstraint("ck_media_assets_width_px", "width_px IS NULL OR width_px > 0");
                    table.ForeignKey(
                        name: "fk_media_assets_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_profiles_tenant_id_logo_media_asset_id",
                table: "tenant_profiles",
                columns: new[] { "tenant_id", "logo_media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_storefront_banners_tenant_id_image_media_asset_id",
                table: "storefront_banners",
                columns: new[] { "tenant_id", "image_media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_option_values_tenant_id_image_media_asset_id",
                table: "product_option_values",
                columns: new[] { "tenant_id", "image_media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_tenant_id_media_asset_id",
                table: "product_images",
                columns: new[] { "tenant_id", "media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_tenant_id_image_media_asset_id",
                table: "categories",
                columns: new[] { "tenant_id", "image_media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_id_logo_media_asset_id",
                table: "brands",
                columns: new[] { "tenant_id", "logo_media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_tenant_id_asset_purpose_status",
                table: "media_assets",
                columns: new[] { "tenant_id", "asset_purpose", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_media_assets_tenant_id_container_name_storage_key",
                table: "media_assets",
                columns: new[] { "tenant_id", "container_name", "storage_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_brands_logo_media_asset_tenant",
                table: "brands",
                columns: new[] { "tenant_id", "logo_media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_categories_image_media_asset_tenant",
                table: "categories",
                columns: new[] { "tenant_id", "image_media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_images_media_asset_tenant",
                table: "product_images",
                columns: new[] { "tenant_id", "media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_values_image_media_asset_tenant",
                table: "product_option_values",
                columns: new[] { "tenant_id", "image_media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_storefront_banners_image_media_asset_tenant",
                table: "storefront_banners",
                columns: new[] { "tenant_id", "image_media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_profiles_logo_media_asset_tenant",
                table: "tenant_profiles",
                columns: new[] { "tenant_id", "logo_media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_brands_logo_media_asset_tenant",
                table: "brands");

            migrationBuilder.DropForeignKey(
                name: "fk_categories_image_media_asset_tenant",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_product_images_media_asset_tenant",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "fk_product_option_values_image_media_asset_tenant",
                table: "product_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_storefront_banners_image_media_asset_tenant",
                table: "storefront_banners");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_profiles_logo_media_asset_tenant",
                table: "tenant_profiles");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropIndex(
                name: "ix_tenant_profiles_tenant_id_logo_media_asset_id",
                table: "tenant_profiles");

            migrationBuilder.DropIndex(
                name: "ix_storefront_banners_tenant_id_image_media_asset_id",
                table: "storefront_banners");

            migrationBuilder.DropIndex(
                name: "ix_product_option_values_tenant_id_image_media_asset_id",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "ix_product_images_tenant_id_media_asset_id",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "ix_categories_tenant_id_image_media_asset_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_brands_tenant_id_logo_media_asset_id",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "logo_media_asset_id",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "image_media_asset_id",
                table: "storefront_banners");

            migrationBuilder.DropColumn(
                name: "image_media_asset_id",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "media_asset_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "image_media_asset_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "logo_media_asset_id",
                table: "brands");
        }
    }
}
