using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule14PricingTaxWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_price_list_id_price_lists",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_sales_channel_id_sales_channels",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_price_list_id_price_lists",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_product_id_products",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_product_variant_id_product_variants",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_outlet_id_outlets",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_price_list_id_price_lists",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_product_id_products",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_product_variant_id_product_variants",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_tax_class_id_tax_classes",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_tax_class_id_tax_classes",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_tax_rate_id_tax_rates",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions",
                table: "tax_jurisdictions");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_rates_tax_jurisdiction_id",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_rate_percent_min",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_jurisdictions_parent_jurisdiction_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropIndex(
                name: "IX_tax_class_rates_tax_class_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_class_rates_tax_rate_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_product_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_product_variant_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_tax_class_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_price_list_outlets_outlet_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "IX_price_list_outlets_price_list_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_price_list_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_product_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_product_variant_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_channels_price_list_id",
                table: "price_list_channels");

            migrationBuilder.DropIndex(
                name: "IX_price_list_channels_sales_channel_id",
                table: "price_list_channels");

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
                name: "CreatedByTenantUserId",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedByTenantUserId",
                table: "product_tax_assignments");

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

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tax_rates_tenant_id_id",
                table: "tax_rates",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tax_jurisdictions_tenant_id_id",
                table: "tax_jurisdictions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tax_classes_tenant_id_id",
                table: "tax_classes",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_sales_channels_tenant_id_id",
                table: "sales_channels",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_price_lists_tenant_id_id",
                table: "price_lists",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_outlets_tenant_id_id",
                table: "outlets",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_tenant_id_tax_jurisdiction_id",
                table: "tax_rates",
                columns: new[] { "tenant_id", "tax_jurisdiction_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_rate_percent_min",
                table: "tax_rates",
                sql: "rate_percent >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_tax_jurisdictions_tenant_id_parent_jurisdiction_id",
                table: "tax_jurisdictions",
                columns: new[] { "tenant_id", "parent_jurisdiction_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_tenant_id_tax_rate_id",
                table: "tax_class_rates",
                columns: new[] { "tenant_id", "tax_rate_id" });

            migrationBuilder.CreateIndex(
                name: "uq_sales_channels_tenant_id_id",
                table: "sales_channels",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_tenant_id_tax_class_id",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "tax_class_id" });

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_tenant_id_outlet_id",
                table: "price_list_outlets",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_tenant_id_product_id",
                table: "price_list_items",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_tenant_id_product_variant_id",
                table: "price_list_items",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_tenant_id_sales_channel_id",
                table: "price_list_channels",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "uq_outlets_tenant_id_id",
                table: "outlets",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_price_list_id_price_lists",
                table: "price_list_channels",
                columns: new[] { "tenant_id", "price_list_id" },
                principalTable: "price_lists",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_sales_channel_id_sales_channels",
                table: "price_list_channels",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_price_list_id_price_lists",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id" },
                principalTable: "price_lists",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_product_id_products",
                table: "price_list_items",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_product_variant_id_product_variants",
                table: "price_list_items",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_outlet_id_outlets",
                table: "price_list_outlets",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_price_list_id_price_lists",
                table: "price_list_outlets",
                columns: new[] { "tenant_id", "price_list_id" },
                principalTable: "price_lists",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_product_id_products",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_product_variant_id_product_variants",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_tax_class_id_tax_classes",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "tax_class_id" },
                principalTable: "tax_classes",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_tax_class_id_tax_classes",
                table: "tax_class_rates",
                columns: new[] { "tenant_id", "tax_class_id" },
                principalTable: "tax_classes",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_tax_rate_id_tax_rates",
                table: "tax_class_rates",
                columns: new[] { "tenant_id", "tax_rate_id" },
                principalTable: "tax_rates",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions",
                table: "tax_jurisdictions",
                columns: new[] { "tenant_id", "parent_jurisdiction_id" },
                principalTable: "tax_jurisdictions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions",
                table: "tax_rates",
                columns: new[] { "tenant_id", "tax_jurisdiction_id" },
                principalTable: "tax_jurisdictions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_price_list_id_price_lists",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_sales_channel_id_sales_channels",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_price_list_id_price_lists",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_product_id_products",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_product_variant_id_product_variants",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_outlet_id_outlets",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_price_list_id_price_lists",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_product_id_products",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_product_variant_id_product_variants",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_tax_class_id_tax_classes",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_tax_class_id_tax_classes",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_tax_rate_id_tax_rates",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions",
                table: "tax_jurisdictions");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions",
                table: "tax_rates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tax_rates_tenant_id_id",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_rates_tenant_id_tax_jurisdiction_id",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_rate_percent_min",
                table: "tax_rates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tax_jurisdictions_tenant_id_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropIndex(
                name: "IX_tax_jurisdictions_tenant_id_parent_jurisdiction_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tax_classes_tenant_id_id",
                table: "tax_classes");

            migrationBuilder.DropIndex(
                name: "IX_tax_class_rates_tenant_id_tax_rate_id",
                table: "tax_class_rates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_sales_channels_tenant_id_id",
                table: "sales_channels");

            migrationBuilder.DropIndex(
                name: "uq_sales_channels_tenant_id_id",
                table: "sales_channels");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_tenant_id_tax_class_id",
                table: "product_tax_assignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_price_lists_tenant_id_id",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_list_outlets_tenant_id_outlet_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_tenant_id_product_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_tenant_id_product_variant_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_channels_tenant_id_sales_channel_id",
                table: "price_list_channels");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_outlets_tenant_id_id",
                table: "outlets");

            migrationBuilder.DropIndex(
                name: "uq_outlets_tenant_id_id",
                table: "outlets");

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

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_tax_jurisdiction_id",
                table: "tax_rates",
                column: "tax_jurisdiction_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_rate_percent_min",
                table: "tax_rates",
                sql: "rate_percent > 0");

            migrationBuilder.CreateIndex(
                name: "IX_tax_jurisdictions_parent_jurisdiction_id",
                table: "tax_jurisdictions",
                column: "parent_jurisdiction_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_tax_class_id",
                table: "tax_class_rates",
                column: "tax_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_tax_rate_id",
                table: "tax_class_rates",
                column: "tax_rate_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_product_id",
                table: "product_tax_assignments",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_product_variant_id",
                table: "product_tax_assignments",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_tax_class_id",
                table: "product_tax_assignments",
                column: "tax_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_outlet_id",
                table: "price_list_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_price_list_id",
                table: "price_list_outlets",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_price_list_id",
                table: "price_list_items",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_product_id",
                table: "price_list_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_product_variant_id",
                table: "price_list_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_price_list_id",
                table: "price_list_channels",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_sales_channel_id",
                table: "price_list_channels",
                column: "sales_channel_id");

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_price_list_id_price_lists",
                table: "price_list_channels",
                column: "price_list_id",
                principalTable: "price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_sales_channel_id_sales_channels",
                table: "price_list_channels",
                column: "sales_channel_id",
                principalTable: "sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_price_list_id_price_lists",
                table: "price_list_items",
                column: "price_list_id",
                principalTable: "price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_product_id_products",
                table: "price_list_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_product_variant_id_product_variants",
                table: "price_list_items",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_outlet_id_outlets",
                table: "price_list_outlets",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_price_list_id_price_lists",
                table: "price_list_outlets",
                column: "price_list_id",
                principalTable: "price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_product_id_products",
                table: "product_tax_assignments",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_product_variant_id_product_variants",
                table: "product_tax_assignments",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_tax_class_id_tax_classes",
                table: "product_tax_assignments",
                column: "tax_class_id",
                principalTable: "tax_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_tax_class_id_tax_classes",
                table: "tax_class_rates",
                column: "tax_class_id",
                principalTable: "tax_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_tax_rate_id_tax_rates",
                table: "tax_class_rates",
                column: "tax_rate_id",
                principalTable: "tax_rates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions",
                table: "tax_jurisdictions",
                column: "parent_jurisdiction_id",
                principalTable: "tax_jurisdictions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_rates_tax_jurisdiction_id_tax_jurisdictions",
                table: "tax_rates",
                column: "tax_jurisdiction_id",
                principalTable: "tax_jurisdictions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
