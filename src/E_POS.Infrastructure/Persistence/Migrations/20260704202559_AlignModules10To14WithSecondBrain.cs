using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModules10To14WithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_business_type_option_templates_product_option_template_id_product_option_templates",
                table: "business_type_option_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_product_channel_visibility_sales_channel_id_sales_channels",
                table: "product_channel_visibility");

            migrationBuilder.DropForeignKey(
                name: "fk_product_option_template_values_product_option_template_id_product_option_templates",
                table: "product_option_template_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_option_templates_tenant_id_tenants",
                table: "product_option_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_product_options_product_option_template_id_product_option_templates",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "uq_product_variant_option_values_product_variant_id_product_option_value_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_options_product_option_template_id",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "uq_product_options_product_id_option_code",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "uq_product_option_values_product_option_id_option_value_code",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_option_templates_tenant_id_template_code",
                table: "product_option_templates");

            migrationBuilder.DropIndex(
                name: "uq_product_images_product_id_image_url",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "uq_product_collections_product_id_collection_id",
                table: "product_collections");

            migrationBuilder.DropIndex(
                name: "IX_product_channel_visibility_sales_channel_id",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "uq_product_channel_visibility_product_id_sales_channel_id",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "uq_product_categories_product_id_category_id",
                table: "product_categories");

            migrationBuilder.DropIndex(
                name: "uq_product_barcodes_tenant_id_barcode_value",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "description",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "barcode_value",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "description",
                table: "business_type_option_templates");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "products",
                newName: "product_name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "products",
                newName: "short_description");

            migrationBuilder.RenameColumn(
                name: "product_option_template_id",
                table: "product_options",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "option_value_code",
                table: "product_option_values",
                newName: "value_code");

            migrationBuilder.RenameColumn(
                name: "product_option_template_id",
                table: "product_option_template_values",
                newName: "option_template_id");

            migrationBuilder.RenameIndex(
                name: "uq_product_option_template_values_product_option_template_id_value_code",
                table: "product_option_template_values",
                newName: "uq_product_option_template_values_option_template_id_value_code");

            migrationBuilder.RenameColumn(
                name: "product_option_template_id",
                table: "business_type_option_templates",
                newName: "option_template_id");

            migrationBuilder.RenameIndex(
                name: "uq_business_type_option_templates_business_type_id_product_option_template_id",
                table: "business_type_option_templates",
                newName: "uq_business_type_option_templates_business_type_id_option_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_business_type_option_templates_product_option_template_id",
                table: "business_type_option_templates",
                newName: "IX_business_type_option_templates_option_template_id");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "tax_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "tax_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "tax_jurisdictions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "tax_jurisdictions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "tax_classes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "tax_classes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "tax_class_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "tax_class_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "products",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "brand_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "business_type_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_taxable",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "long_description",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_slug",
                table: "products",
                type: "varchar(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_structure",
                table: "products",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_type",
                table: "products",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "return_policy_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "product_variants",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "sku",
                table: "product_variants",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "allow_fractional_quantity",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_variants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_variant",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "option_combination_hash",
                table: "product_variants",
                type: "char(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_uom_id",
                table: "product_variants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "stock_uom_id",
                table: "product_variants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_variants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variant_name",
                table: "product_variants",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "product_option_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_options",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_type",
                table: "product_options",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "product_options",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "option_name",
                table: "product_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "option_type",
                table: "product_options",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "source_option_template_id",
                table: "product_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_options",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_option_values",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "color_hex",
                table: "product_option_values",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_option_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "product_option_values",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "product_option_values",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_option_template_value_id",
                table: "product_option_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_option_values",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_option_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_option_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_name",
                table: "product_option_values",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "product_option_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_type",
                table: "product_option_templates",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "option_type",
                table: "product_option_templates",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "product_option_templates",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "product_option_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_option_template_values",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "color_hex",
                table: "product_option_template_values",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "product_option_template_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "product_option_template_values",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "product_option_template_values",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_option_template_values",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "product_option_template_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_name",
                table: "product_option_template_values",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_images",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "product_images",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "alt_text",
                table: "product_images",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checksum_hash",
                table: "product_images",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "file_size_bytes",
                table: "product_images",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "height_px",
                table: "product_images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_purpose",
                table: "product_images",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "image_storage_key",
                table: "product_images",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_primary_image",
                table: "product_images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "mime_type",
                table: "product_images",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_channel_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_images",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_images",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "width_px",
                table: "product_images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_collections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_collections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_collections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "available_from",
                table: "product_channel_visibility",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "available_until",
                table: "product_channel_visibility",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_channel_visibility",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_orderable",
                table: "product_channel_visibility",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_visible",
                table: "product_channel_visibility",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "product_channel_visibility",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_channel_visibility",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_channel_visibility",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_channel_visibility",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary_category",
                table: "product_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barcode",
                table: "product_barcodes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "barcode_type",
                table: "product_barcodes",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_barcodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary_barcode",
                table: "product_barcodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_per_scan",
                table: "product_barcodes",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_barcodes",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "uom_id",
                table: "product_barcodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_barcodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "price_list_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "price_list_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "price_list_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "price_list_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByTenantUserId",
                table: "price_list_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByTenantUserId",
                table: "price_list_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "business_type_option_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_template",
                table: "business_type_option_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "business_type_option_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_products_tenant_id_product_slug",
                table: "products",
                columns: new[] { "tenant_id", "product_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_sales_uom_id",
                table: "product_variants",
                column: "sales_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_stock_uom_id",
                table: "product_variants",
                column: "stock_uom_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_variants_tenant_id_product_id_option_combination_hash",
                table: "product_variants",
                columns: new[] { "tenant_id", "product_id", "option_combination_hash" },
                unique: true,
                filter: "option_combination_hash IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_id",
                table: "product_variant_option_values",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_option_id",
                table: "product_variant_option_values",
                column: "product_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_variant_id",
                table: "product_variant_option_values",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_variant_option_values_tenant_id_variant_id_option_id",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_variant_id", "product_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_options_product_id",
                table: "product_options",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_options_source_option_template_id",
                table: "product_options",
                column: "source_option_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_options_tenant_id_product_id_option_code",
                table: "product_options",
                columns: new[] { "tenant_id", "product_id", "option_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_options_status",
                table: "product_options",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_option_values_product_option_id",
                table: "product_option_values",
                column: "product_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_option_values_source_option_template_value_id",
                table: "product_option_values",
                column: "source_option_template_value_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_option_values_tenant_id_product_option_id_value_code",
                table: "product_option_values",
                columns: new[] { "tenant_id", "product_option_id", "value_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_option_values_status",
                table: "product_option_values",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_product_option_templates_template_code",
                table: "product_option_templates",
                column: "template_code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_option_templates_sort_order",
                table: "product_option_templates",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_option_templates_status",
                table: "product_option_templates",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_option_template_values_status",
                table: "product_option_template_values",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_variant_id",
                table: "product_images",
                column: "product_variant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_images_status",
                table: "product_images",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_collections_product_id",
                table: "product_collections",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_collections_tenant_id_product_id_collection_id",
                table: "product_collections",
                columns: new[] { "tenant_id", "product_id", "collection_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_collections_sort_order",
                table: "product_collections",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_product_channel_visibility_product_id",
                table: "product_channel_visibility",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_channel_visibility_product_variant_id",
                table: "product_channel_visibility",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_channel_visibility_tenant_id_product_id_channel_id",
                table: "product_channel_visibility",
                columns: new[] { "tenant_id", "product_id", "sales_channel_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_channel_visibility_tenant_id_variant_id_channel_id",
                table: "product_channel_visibility",
                columns: new[] { "tenant_id", "product_variant_id", "sales_channel_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_channel_visibility_status",
                table: "product_channel_visibility",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_product_id",
                table: "product_categories",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_categories_tenant_id_product_id_category_id",
                table: "product_categories",
                columns: new[] { "tenant_id", "product_id", "category_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_categories_sort_order",
                table: "product_categories",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_product_barcodes_tenant_id_barcode",
                table: "product_barcodes",
                columns: new[] { "tenant_id", "barcode" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_barcodes_quantity_per_scan",
                table: "product_barcodes",
                sql: "quantity_per_scan > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_barcodes_status",
                table: "product_barcodes",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_business_type_option_templates_sort_order",
                table: "business_type_option_templates",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_business_type_option_templates_status",
                table: "business_type_option_templates",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_business_type_option_templates_option_template_id_product_option_templates",
                table: "business_type_option_templates",
                column: "option_template_id",
                principalTable: "product_option_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_channel_visibility_product_variant_id_product_variants",
                table: "product_channel_visibility",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_images_product_variant_id_product_variants",
                table: "product_images",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_template_values_option_template_id_product_option_templates",
                table: "product_option_template_values",
                column: "option_template_id",
                principalTable: "product_option_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_values_source_option_template_value_id_product_option_template_values",
                table: "product_option_values",
                column: "source_option_template_value_id",
                principalTable: "product_option_template_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_options_source_option_template_id_product_option_templates",
                table: "product_options",
                column: "source_option_template_id",
                principalTable: "product_option_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_id_products",
                table: "product_variant_option_values",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_option_id_product_options",
                table: "product_variant_option_values",
                column: "product_option_id",
                principalTable: "product_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variants_sales_uom_id_unit_of_measures",
                table: "product_variants",
                column: "sales_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variants_stock_uom_id_unit_of_measures",
                table: "product_variants",
                column: "stock_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_business_type_option_templates_option_template_id_product_option_templates",
                table: "business_type_option_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_product_channel_visibility_product_variant_id_product_variants",
                table: "product_channel_visibility");

            migrationBuilder.DropForeignKey(
                name: "fk_product_images_product_variant_id_product_variants",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "fk_product_option_template_values_option_template_id_product_option_templates",
                table: "product_option_template_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_option_values_source_option_template_value_id_product_option_template_values",
                table: "product_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_options_source_option_template_id_product_option_templates",
                table: "product_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_id_products",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_option_id_product_options",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variants_sales_uom_id_unit_of_measures",
                table: "product_variants");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variants_stock_uom_id_unit_of_measures",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "uq_products_tenant_id_product_slug",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_sales_uom_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_stock_uom_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "uq_product_variants_tenant_id_product_id_option_combination_hash",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_option_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_variant_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_variant_option_values_tenant_id_variant_id_option_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_options_product_id",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "IX_product_options_source_option_template_id",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "uq_product_options_tenant_id_product_id_option_code",
                table: "product_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_options_status",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "IX_product_option_values_product_option_id",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_option_values_source_option_template_value_id",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_option_values_tenant_id_product_option_id_value_code",
                table: "product_option_values");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_option_values_status",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_option_templates_template_code",
                table: "product_option_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_option_templates_sort_order",
                table: "product_option_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_option_templates_status",
                table: "product_option_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_option_template_values_status",
                table: "product_option_template_values");

            migrationBuilder.DropIndex(
                name: "IX_product_images_product_id",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "IX_product_images_product_variant_id",
                table: "product_images");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_images_status",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "IX_product_collections_product_id",
                table: "product_collections");

            migrationBuilder.DropIndex(
                name: "uq_product_collections_tenant_id_product_id_collection_id",
                table: "product_collections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_collections_sort_order",
                table: "product_collections");

            migrationBuilder.DropIndex(
                name: "IX_product_channel_visibility_product_id",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "IX_product_channel_visibility_product_variant_id",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "uq_product_channel_visibility_tenant_id_product_id_channel_id",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "uq_product_channel_visibility_tenant_id_variant_id_channel_id",
                table: "product_channel_visibility");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_channel_visibility_status",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "IX_product_categories_product_id",
                table: "product_categories");

            migrationBuilder.DropIndex(
                name: "uq_product_categories_tenant_id_product_id_category_id",
                table: "product_categories");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_categories_sort_order",
                table: "product_categories");

            migrationBuilder.DropIndex(
                name: "uq_product_barcodes_tenant_id_barcode",
                table: "product_barcodes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_barcodes_quantity_per_scan",
                table: "product_barcodes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_barcodes_status",
                table: "product_barcodes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_business_type_option_templates_sort_order",
                table: "business_type_option_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_business_type_option_templates_status",
                table: "business_type_option_templates");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "brand_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "business_type_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_taxable",
                table: "products");

            migrationBuilder.DropColumn(
                name: "long_description",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_slug",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_structure",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_type",
                table: "products");

            migrationBuilder.DropColumn(
                name: "return_policy_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "allow_fractional_quantity",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "is_default_variant",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "option_combination_hash",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "sales_uom_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "stock_uom_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "variant_name",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_variant_option_values");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "product_variant_option_values");

            migrationBuilder.DropColumn(
                name: "product_option_id",
                table: "product_variant_option_values");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_variant_option_values");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_variant_option_values");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "input_type",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "is_required",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "option_name",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "option_type",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "source_option_template_id",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_options");

            migrationBuilder.DropColumn(
                name: "color_hex",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "source_option_template_value_id",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "value_name",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "input_type",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "option_type",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "product_option_templates");

            migrationBuilder.DropColumn(
                name: "color_hex",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "value_name",
                table: "product_option_template_values");

            migrationBuilder.DropColumn(
                name: "alt_text",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "checksum_hash",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "file_size_bytes",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "height_px",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "image_purpose",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "image_storage_key",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "is_primary_image",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "mime_type",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "sales_channel_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "width_px",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_collections");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_collections");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_collections");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_collections");

            migrationBuilder.DropColumn(
                name: "available_from",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "available_until",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "is_orderable",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "is_visible",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_channel_visibility");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "is_primary_category",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "barcode",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "barcode_type",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "is_primary_barcode",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "quantity_per_scan",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "uom_id",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_barcodes");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "price_list_outlets");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "price_list_outlets");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "CreatedByTenantUserId",
                table: "price_list_channels");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "price_list_channels");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "business_type_option_templates");

            migrationBuilder.DropColumn(
                name: "is_default_template",
                table: "business_type_option_templates");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "business_type_option_templates");

            migrationBuilder.RenameColumn(
                name: "short_description",
                table: "products",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "product_name",
                table: "products",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "product_options",
                newName: "product_option_template_id");

            migrationBuilder.RenameColumn(
                name: "value_code",
                table: "product_option_values",
                newName: "option_value_code");

            migrationBuilder.RenameColumn(
                name: "option_template_id",
                table: "product_option_template_values",
                newName: "product_option_template_id");

            migrationBuilder.RenameIndex(
                name: "uq_product_option_template_values_option_template_id_value_code",
                table: "product_option_template_values",
                newName: "uq_product_option_template_values_product_option_template_id_value_code");

            migrationBuilder.RenameColumn(
                name: "option_template_id",
                table: "business_type_option_templates",
                newName: "product_option_template_id");

            migrationBuilder.RenameIndex(
                name: "uq_business_type_option_templates_business_type_id_option_template_id",
                table: "business_type_option_templates",
                newName: "uq_business_type_option_templates_business_type_id_product_option_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_business_type_option_templates_option_template_id",
                table: "business_type_option_templates",
                newName: "IX_business_type_option_templates_product_option_template_id");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "products",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "product_variants",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "sku",
                table: "product_variants",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_variants",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "product_variant_option_values",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_options",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_options",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_option_values",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_option_values",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "product_option_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_option_templates",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_option_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_option_template_values",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_option_template_values",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "product_images",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "product_images",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barcode_value",
                table: "product_barcodes",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "business_type_option_templates",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_variant_option_values_product_variant_id_product_option_value_id",
                table: "product_variant_option_values",
                columns: new[] { "product_variant_id", "product_option_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_options_product_option_template_id",
                table: "product_options",
                column: "product_option_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_options_product_id_option_code",
                table: "product_options",
                columns: new[] { "product_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_values_product_option_id_option_value_code",
                table: "product_option_values",
                columns: new[] { "product_option_id", "option_value_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_templates_tenant_id_template_code",
                table: "product_option_templates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_images_product_id_image_url",
                table: "product_images",
                columns: new[] { "product_id", "image_url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_collections_product_id_collection_id",
                table: "product_collections",
                columns: new[] { "product_id", "collection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_channel_visibility_sales_channel_id",
                table: "product_channel_visibility",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_channel_visibility_product_id_sales_channel_id",
                table: "product_channel_visibility",
                columns: new[] { "product_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_categories_product_id_category_id",
                table: "product_categories",
                columns: new[] { "product_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_barcodes_tenant_id_barcode_value",
                table: "product_barcodes",
                columns: new[] { "tenant_id", "barcode_value" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_business_type_option_templates_product_option_template_id_product_option_templates",
                table: "business_type_option_templates",
                column: "product_option_template_id",
                principalTable: "product_option_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_channel_visibility_sales_channel_id_sales_channels",
                table: "product_channel_visibility",
                column: "sales_channel_id",
                principalTable: "sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_template_values_product_option_template_id_product_option_templates",
                table: "product_option_template_values",
                column: "product_option_template_id",
                principalTable: "product_option_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_templates_tenant_id_tenants",
                table: "product_option_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_options_product_option_template_id_product_option_templates",
                table: "product_options",
                column: "product_option_template_id",
                principalTable: "product_option_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
