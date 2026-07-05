using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule11ProductAttributesWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_options_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_value_options_attribute_option_id_product_attribute_options",
                table: "product_attribute_value_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values",
                table: "product_attribute_value_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_product_id_products",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_values_attribute_definition_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_values_product_id_attribute_definition_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_value_options_attribute_option_id",
                table: "product_attribute_value_options");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_value_options_product_attribute_value_id_attribute_option_id",
                table: "product_attribute_value_options");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_options_attribute_definition_id_option_code",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_attribute_value_options");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "name",
                table: "product_attribute_definitions");

            migrationBuilder.RenameColumn(
                name: "attribute_code",
                table: "product_attribute_definitions",
                newName: "attribute_key");

            migrationBuilder.RenameIndex(
                name: "uq_product_attribute_definitions_tenant_id_attribute_code",
                table: "product_attribute_definitions",
                newName: "uq_product_attribute_definitions_tenant_id_attribute_key");

            migrationBuilder.AddColumn<bool>(
                name: "attribute_value_boolean",
                table: "product_attribute_values",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "attribute_value_date",
                table: "product_attribute_values",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "attribute_value_number",
                table: "product_attribute_values",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "attribute_value_text",
                table: "product_attribute_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "attribute_value_uom_id",
                table: "product_attribute_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_attribute_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "product_attribute_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_attribute_values",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_attribute_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_attribute_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "attribute_definition_id",
                table: "product_attribute_value_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_attribute_value_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_attribute_value_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_attribute_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "option_label",
                table: "product_attribute_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_attribute_options",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_attribute_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_attribute_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "applies_to",
                table: "product_attribute_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "attribute_name",
                table: "product_attribute_definitions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "attribute_type",
                table: "product_attribute_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_attribute_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "default_uom_id",
                table: "product_attribute_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_filterable",
                table: "product_attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "product_attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_searchable",
                table: "product_attribute_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_attribute_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_attribute_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_attribute_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_products_tenant_id_id",
                table: "products",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_variants_tenant_id_id",
                table: "product_variants",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_attribute_values_tenant_id_id_attribute_definition_~",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "id", "attribute_definition_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_attribute_options_tenant_id_attribute_definition_id~",
                table: "product_attribute_options",
                columns: new[] { "tenant_id", "attribute_definition_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_attribute_definitions_tenant_id_id",
                table: "product_attribute_definitions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_channel_visibility_available_dates",
                table: "product_channel_visibility",
                sql: "available_until IS NULL OR available_from IS NULL OR available_until >= available_from");

            migrationBuilder.CreateIndex(
                name: "uq_product_categories_tenant_id_product_id_primary",
                table: "product_categories",
                columns: new[] { "tenant_id", "product_id" },
                unique: true,
                filter: "is_primary_category = true");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_values_attribute_value_uom_id",
                table: "product_attribute_values",
                column: "attribute_value_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_values_tenant_id_attribute_definition_id",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "attribute_definition_id" });

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_values_tenant_id_id",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_values_tenant_id_id_attr_def_id",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "id", "attribute_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_values_tenant_id_product_id_attr_def_id",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "product_id", "attribute_definition_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_values_tenant_id_variant_id_attr_def_id",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "product_variant_id", "attribute_definition_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_values_nonnull_values",
                table: "product_attribute_values",
                sql: "(CASE WHEN attribute_value_text IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN attribute_value_number IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN attribute_value_boolean IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN attribute_value_date IS NOT NULL THEN 1 ELSE 0 END) <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_values_status",
                table: "product_attribute_values",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_values_uom_requires_number",
                table: "product_attribute_values",
                sql: "attribute_value_uom_id IS NULL OR attribute_value_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_value_options_tenant_id_attribute_definit~",
                table: "product_attribute_value_options",
                columns: new[] { "tenant_id", "attribute_definition_id", "attribute_option_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_value_options_tenant_id_product_attribute~",
                table: "product_attribute_value_options",
                columns: new[] { "tenant_id", "product_attribute_value_id", "attribute_definition_id" });

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_val_opts_tenant_id_val_id_opt_id",
                table: "product_attribute_value_options",
                columns: new[] { "tenant_id", "product_attribute_value_id", "attribute_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_options_tenant_id_attribute_def_id_id",
                table: "product_attribute_options",
                columns: new[] { "tenant_id", "attribute_definition_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_options_tenant_id_attribute_def_id_opt_code",
                table: "product_attribute_options",
                columns: new[] { "tenant_id", "attribute_definition_id", "option_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_options_sort_order",
                table: "product_attribute_options",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_options_status",
                table: "product_attribute_options",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_definitions_default_uom_id",
                table: "product_attribute_definitions",
                column: "default_uom_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_definitions_tenant_id_id",
                table: "product_attribute_definitions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_definitions_sort_order",
                table: "product_attribute_definitions",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_attribute_definitions_status",
                table: "product_attribute_definitions",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_definitions_default_uom_id_unit_of_measures",
                table: "product_attribute_definitions",
                column: "default_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_options_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_options",
                columns: new[] { "tenant_id", "attribute_definition_id" },
                principalTable: "product_attribute_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_options_tenant_id_tenants",
                table: "product_attribute_options",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_value_options_attribute_option_id_product_attribute_options",
                table: "product_attribute_value_options",
                columns: new[] { "tenant_id", "attribute_definition_id", "attribute_option_id" },
                principalTable: "product_attribute_options",
                principalColumns: new[] { "tenant_id", "attribute_definition_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values",
                table: "product_attribute_value_options",
                columns: new[] { "tenant_id", "product_attribute_value_id", "attribute_definition_id" },
                principalTable: "product_attribute_values",
                principalColumns: new[] { "tenant_id", "id", "attribute_definition_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_value_options_tenant_id_tenants",
                table: "product_attribute_value_options",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "attribute_definition_id" },
                principalTable: "product_attribute_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_attribute_value_uom_id_unit_of_measures",
                table: "product_attribute_values",
                column: "attribute_value_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_product_id_products",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_product_variant_id_product_variants",
                table: "product_attribute_values",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_tenant_id_tenants",
                table: "product_attribute_values",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_definitions_default_uom_id_unit_of_measures",
                table: "product_attribute_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_options_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_options_tenant_id_tenants",
                table: "product_attribute_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_value_options_attribute_option_id_product_attribute_options",
                table: "product_attribute_value_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values",
                table: "product_attribute_value_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_value_options_tenant_id_tenants",
                table: "product_attribute_value_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_attribute_value_uom_id_unit_of_measures",
                table: "product_attribute_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_product_id_products",
                table: "product_attribute_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_product_variant_id_product_variants",
                table: "product_attribute_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_attribute_values_tenant_id_tenants",
                table: "product_attribute_values");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_products_tenant_id_id",
                table: "products");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_variants_tenant_id_id",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_channel_visibility_available_dates",
                table: "product_channel_visibility");

            migrationBuilder.DropIndex(
                name: "uq_product_categories_tenant_id_product_id_primary",
                table: "product_categories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_attribute_values_tenant_id_id_attribute_definition_~",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_values_attribute_value_uom_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_values_tenant_id_attribute_definition_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_values_tenant_id_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_values_tenant_id_id_attr_def_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_values_tenant_id_product_id_attr_def_id",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_values_tenant_id_variant_id_attr_def_id",
                table: "product_attribute_values");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_values_nonnull_values",
                table: "product_attribute_values");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_values_status",
                table: "product_attribute_values");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_values_uom_requires_number",
                table: "product_attribute_values");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_value_options_tenant_id_attribute_definit~",
                table: "product_attribute_value_options");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_value_options_tenant_id_product_attribute~",
                table: "product_attribute_value_options");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_val_opts_tenant_id_val_id_opt_id",
                table: "product_attribute_value_options");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_attribute_options_tenant_id_attribute_definition_id~",
                table: "product_attribute_options");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_options_tenant_id_attribute_def_id_id",
                table: "product_attribute_options");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_options_tenant_id_attribute_def_id_opt_code",
                table: "product_attribute_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_options_sort_order",
                table: "product_attribute_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_options_status",
                table: "product_attribute_options");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_attribute_definitions_tenant_id_id",
                table: "product_attribute_definitions");

            migrationBuilder.DropIndex(
                name: "IX_product_attribute_definitions_default_uom_id",
                table: "product_attribute_definitions");

            migrationBuilder.DropIndex(
                name: "uq_product_attribute_definitions_tenant_id_id",
                table: "product_attribute_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_definitions_sort_order",
                table: "product_attribute_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_attribute_definitions_status",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "attribute_value_boolean",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "attribute_value_date",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "attribute_value_number",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "attribute_value_text",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "attribute_value_uom_id",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "attribute_definition_id",
                table: "product_attribute_value_options");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_attribute_value_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_attribute_value_options");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "option_label",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_attribute_options");

            migrationBuilder.DropColumn(
                name: "applies_to",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "attribute_name",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "attribute_type",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "default_uom_id",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_filterable",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_required",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "is_searchable",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_attribute_definitions");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_attribute_definitions");

            migrationBuilder.RenameColumn(
                name: "attribute_key",
                table: "product_attribute_definitions",
                newName: "attribute_code");

            migrationBuilder.RenameIndex(
                name: "uq_product_attribute_definitions_tenant_id_attribute_key",
                table: "product_attribute_definitions",
                newName: "uq_product_attribute_definitions_tenant_id_attribute_code");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_attribute_value_options",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_attribute_options",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "product_attribute_definitions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_values_attribute_definition_id",
                table: "product_attribute_values",
                column: "attribute_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_values_product_id_attribute_definition_id",
                table: "product_attribute_values",
                columns: new[] { "product_id", "attribute_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_value_options_attribute_option_id",
                table: "product_attribute_value_options",
                column: "attribute_option_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_value_options_product_attribute_value_id_attribute_option_id",
                table: "product_attribute_value_options",
                columns: new[] { "product_attribute_value_id", "attribute_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_attribute_options_attribute_definition_id_option_code",
                table: "product_attribute_options",
                columns: new[] { "attribute_definition_id", "option_code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_options_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_options",
                column: "attribute_definition_id",
                principalTable: "product_attribute_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_value_options_attribute_option_id_product_attribute_options",
                table: "product_attribute_value_options",
                column: "attribute_option_id",
                principalTable: "product_attribute_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_value_options_product_attribute_value_id_product_attribute_values",
                table: "product_attribute_value_options",
                column: "product_attribute_value_id",
                principalTable: "product_attribute_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_attribute_definition_id_product_attribute_definitions",
                table: "product_attribute_values",
                column: "attribute_definition_id",
                principalTable: "product_attribute_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_attribute_values_product_id_products",
                table: "product_attribute_values",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
