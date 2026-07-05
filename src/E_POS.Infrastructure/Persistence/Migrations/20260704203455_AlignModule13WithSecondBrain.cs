using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule13WithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_ingredient_product_id_products",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id_product_choice_options",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_product_id_products",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_options_product_choice_group_id_choice_option_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_groups_product_id_choice_group_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "uq_combo_groups_combo_definition_id_group_code",
                table: "combo_groups");

            migrationBuilder.DropIndex(
                name: "uq_combo_group_items_combo_group_id_product_id_product_variant_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "uq_combo_definitions_tenant_id_product_id_combo_code",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "uq_combo_components_combo_definition_id_component_product_id_component_variant_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "uq_choice_options_choice_group_id_option_code",
                table: "choice_options");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_ingredient_product_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_option_inventory_impacts_quantity_delta",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "name",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "name",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "quantity_delta",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "name",
                table: "choice_groups");

            migrationBuilder.RenameColumn(
                name: "product_variant_id",
                table: "combo_group_items",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "combo_group_items",
                newName: "item_uom_id");

            migrationBuilder.RenameIndex(
                name: "IX_combo_group_items_product_id",
                table: "combo_group_items",
                newName: "IX_combo_group_items_item_uom_id");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "combo_definitions",
                newName: "combo_name");

            migrationBuilder.RenameColumn(
                name: "ingredient_product_id",
                table: "choice_option_inventory_impacts",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "choice_group_code",
                table: "choice_groups",
                newName: "group_code");

            migrationBuilder.RenameIndex(
                name: "uq_choice_groups_tenant_id_choice_group_code",
                table: "choice_groups",
                newName: "uq_choice_groups_tenant_id_group_code");

            migrationBuilder.AddColumn<Guid>(
                name: "choice_group_id",
                table: "product_choice_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_choice_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "product_choice_options",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_option",
                table: "product_choice_options",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "price_adjustment_override",
                table: "product_choice_options",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order_override",
                table: "product_choice_options",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_choice_options",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_choice_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_choice_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_choice_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_select_override",
                table: "product_choice_groups",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_select_override",
                table: "product_choice_groups",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "product_choice_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_choice_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_choice_groups",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_choice_groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_choice_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "min_select",
                table: "combo_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "combo_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_name",
                table: "combo_groups",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "combo_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "combo_groups",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "combo_groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "combo_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "combo_group_items",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "base_price_adjustment",
                table: "combo_group_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "combo_group_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_item",
                table: "combo_group_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "item_product_id",
                table: "combo_group_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "item_variant_id",
                table: "combo_group_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "combo_group_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "combo_group_items",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "combo_group_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "combo_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "combo_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inventory_deduction_mode",
                table: "combo_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pricing_mode",
                table: "combo_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "combo_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "combo_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "component_variant_id",
                table: "combo_components",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "base_price_adjustment",
                table: "combo_components",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "component_uom_id",
                table: "combo_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "combo_components",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "combo_components",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "combo_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "combo_components",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "choice_options",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "choice_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "default_price_adjustment",
                table: "choice_options",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "option_name",
                table: "choice_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "choice_options",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "choice_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "choice_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "choice_option_inventory_impacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "impact_product_id",
                table: "choice_option_inventory_impacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "impact_uom_id",
                table: "choice_option_inventory_impacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "impact_variant_id",
                table: "choice_option_inventory_impacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inventory_effect_type",
                table: "choice_option_inventory_impacts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "choice_option_inventory_impacts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "choice_option_inventory_impacts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "choice_option_inventory_impacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "min_select",
                table: "choice_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "choice_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_name",
                table: "choice_groups",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "choice_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "choice_groups",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "choice_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_choice_group_id",
                table: "product_choice_options",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_product_choice_group_id",
                table: "product_choice_options",
                column: "product_choice_group_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_options_tenant_id_prod_choice_group_option",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "product_choice_group_id", "choice_option_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_options_sort_order",
                table: "product_choice_options",
                sql: "sort_order_override IS NULL OR sort_order_override >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_options_status",
                table: "product_choice_options",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_product_id",
                table: "product_choice_groups",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_product_variant_id",
                table: "product_choice_groups",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_groups_tenant_id_product_id_choice_group_id",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "product_id", "choice_group_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_groups_tenant_id_variant_id_choice_group_id",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "product_variant_id", "choice_group_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_groups_max_select",
                table: "product_choice_groups",
                sql: "max_select_override IS NULL OR max_select_override > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_groups_max_select_min_select",
                table: "product_choice_groups",
                sql: "max_select_override IS NULL OR min_select_override IS NULL OR max_select_override >= min_select_override");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_groups_min_select",
                table: "product_choice_groups",
                sql: "min_select_override IS NULL OR min_select_override >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_groups_sort_order",
                table: "product_choice_groups",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_choice_groups_status",
                table: "product_choice_groups",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_combo_groups_combo_definition_id",
                table: "combo_groups",
                column: "combo_definition_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_groups_tenant_id_combo_definition_id_group_code",
                table: "combo_groups",
                columns: new[] { "tenant_id", "combo_definition_id", "group_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_groups_max_select",
                table: "combo_groups",
                sql: "max_select > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_groups_sort_order",
                table: "combo_groups",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_groups_status",
                table: "combo_groups",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_item_product_id",
                table: "combo_group_items",
                column: "item_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_item_variant_id",
                table: "combo_group_items",
                column: "item_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_group_items_combo_group_id_item_product_id_uom_id",
                table: "combo_group_items",
                columns: new[] { "combo_group_id", "item_product_id", "item_uom_id" },
                unique: true,
                filter: "item_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_combo_group_items_combo_group_id_item_variant_id_uom_id",
                table: "combo_group_items",
                columns: new[] { "combo_group_id", "item_variant_id", "item_uom_id" },
                unique: true,
                filter: "item_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_group_items_quantity",
                table: "combo_group_items",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_group_items_status",
                table: "combo_group_items",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_combo_definitions_product_variant_id",
                table: "combo_definitions",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_definitions_tenant_id_product_id_combo_code",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "product_id", "combo_code" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_combo_definitions_tenant_id_product_variant_id_combo_code",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "product_variant_id", "combo_code" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_component_uom_id",
                table: "combo_components",
                column: "component_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_component_variant_id",
                table: "combo_components",
                column: "component_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_combo_components_combo_definition_id_comp_product_uom",
                table: "combo_components",
                columns: new[] { "combo_definition_id", "component_product_id", "component_uom_id" },
                unique: true,
                filter: "component_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_combo_components_combo_definition_id_comp_variant_uom",
                table: "combo_components",
                columns: new[] { "combo_definition_id", "component_variant_id", "component_uom_id" },
                unique: true,
                filter: "component_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_components_sort_order",
                table: "combo_components",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_combo_components_status",
                table: "combo_components",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_choice_options_choice_group_id",
                table: "choice_options",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "uq_choice_options_tenant_id_choice_group_id_option_code",
                table: "choice_options",
                columns: new[] { "tenant_id", "choice_group_id", "option_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_options_status",
                table: "choice_options",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_impact_product_id",
                table: "choice_option_inventory_impacts",
                column: "impact_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_impact_uom_id",
                table: "choice_option_inventory_impacts",
                column: "impact_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_impact_variant_id",
                table: "choice_option_inventory_impacts",
                column: "impact_variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_choice_option_inventory_impacts_product_option_product_uom",
                table: "choice_option_inventory_impacts",
                columns: new[] { "product_choice_option_id", "impact_product_id", "impact_uom_id", "inventory_effect_type" },
                unique: true,
                filter: "impact_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_choice_option_inventory_impacts_product_option_variant_uom",
                table: "choice_option_inventory_impacts",
                columns: new[] { "product_choice_option_id", "impact_variant_id", "impact_uom_id", "inventory_effect_type" },
                unique: true,
                filter: "impact_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_option_inventory_impacts_quantity",
                table: "choice_option_inventory_impacts",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_option_inventory_impacts_status",
                table: "choice_option_inventory_impacts",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_groups_max_select",
                table: "choice_groups",
                sql: "max_select > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_groups_sort_order",
                table: "choice_groups",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_groups_status",
                table: "choice_groups",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_product_id_products",
                table: "choice_option_inventory_impacts",
                column: "impact_product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_uom_id_uoms",
                table: "choice_option_inventory_impacts",
                column: "impact_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_variant_id_variants",
                table: "choice_option_inventory_impacts",
                column: "impact_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts",
                column: "product_choice_option_id",
                principalTable: "product_choice_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_component_uom_id_unit_of_measures",
                table: "combo_components",
                column: "component_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_component_variant_id_product_variants",
                table: "combo_components",
                column: "component_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_definitions_product_variant_id_product_variants",
                table: "combo_definitions",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_item_product_id_products",
                table: "combo_group_items",
                column: "item_product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_item_uom_id_unit_of_measures",
                table: "combo_group_items",
                column: "item_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_item_variant_id_product_variants",
                table: "combo_group_items",
                column: "item_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_groups_product_variant_id_product_variants",
                table: "product_choice_groups",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_options_choice_group_id_choice_groups",
                table: "product_choice_options",
                column: "choice_group_id",
                principalTable: "choice_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_product_id_products",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_uom_id_uoms",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_variant_id_variants",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_component_uom_id_unit_of_measures",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_component_variant_id_product_variants",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_definitions_product_variant_id_product_variants",
                table: "combo_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_product_id_products",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_uom_id_unit_of_measures",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_variant_id_product_variants",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_product_variant_id_product_variants",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_choice_group_id_choice_groups",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_choice_group_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_product_choice_group_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_options_tenant_id_prod_choice_group_option",
                table: "product_choice_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_options_sort_order",
                table: "product_choice_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_options_status",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_groups_product_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_groups_product_variant_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_groups_tenant_id_product_id_choice_group_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_groups_tenant_id_variant_id_choice_group_id",
                table: "product_choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_groups_max_select",
                table: "product_choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_groups_max_select_min_select",
                table: "product_choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_groups_min_select",
                table: "product_choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_groups_sort_order",
                table: "product_choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_choice_groups_status",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "IX_combo_groups_combo_definition_id",
                table: "combo_groups");

            migrationBuilder.DropIndex(
                name: "uq_combo_groups_tenant_id_combo_definition_id_group_code",
                table: "combo_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_groups_max_select",
                table: "combo_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_groups_sort_order",
                table: "combo_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_groups_status",
                table: "combo_groups");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_item_product_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_item_variant_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "uq_combo_group_items_combo_group_id_item_product_id_uom_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "uq_combo_group_items_combo_group_id_item_variant_id_uom_id",
                table: "combo_group_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_group_items_quantity",
                table: "combo_group_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_group_items_status",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "IX_combo_definitions_product_variant_id",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "uq_combo_definitions_tenant_id_product_id_combo_code",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "uq_combo_definitions_tenant_id_product_variant_id_combo_code",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_component_uom_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_component_variant_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "uq_combo_components_combo_definition_id_comp_product_uom",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "uq_combo_components_combo_definition_id_comp_variant_uom",
                table: "combo_components");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_components_sort_order",
                table: "combo_components");

            migrationBuilder.DropCheckConstraint(
                name: "ck_combo_components_status",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "IX_choice_options_choice_group_id",
                table: "choice_options");

            migrationBuilder.DropIndex(
                name: "uq_choice_options_tenant_id_choice_group_id_option_code",
                table: "choice_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_options_status",
                table: "choice_options");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_impact_product_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_impact_uom_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_impact_variant_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "uq_choice_option_inventory_impacts_product_option_product_uom",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "uq_choice_option_inventory_impacts_product_option_variant_uom",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_option_inventory_impacts_quantity",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_option_inventory_impacts_status",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_groups_max_select",
                table: "choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_groups_sort_order",
                table: "choice_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_choice_groups_status",
                table: "choice_groups");

            migrationBuilder.DropColumn(
                name: "choice_group_id",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "is_available",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "is_default_option",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "price_adjustment_override",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "sort_order_override",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_choice_options");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "max_select_override",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "min_select_override",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_choice_groups");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "group_name",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "status",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "combo_groups");

            migrationBuilder.DropColumn(
                name: "base_price_adjustment",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "is_default_item",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "item_product_id",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "item_variant_id",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "status",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "combo_group_items");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "combo_definitions");

            migrationBuilder.DropColumn(
                name: "inventory_deduction_mode",
                table: "combo_definitions");

            migrationBuilder.DropColumn(
                name: "pricing_mode",
                table: "combo_definitions");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "combo_definitions");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "combo_definitions");

            migrationBuilder.DropColumn(
                name: "base_price_adjustment",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "component_uom_id",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "status",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "default_price_adjustment",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "option_name",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "status",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "choice_options");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "impact_product_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "impact_uom_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "impact_variant_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "inventory_effect_type",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "status",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "choice_groups");

            migrationBuilder.DropColumn(
                name: "group_name",
                table: "choice_groups");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "choice_groups");

            migrationBuilder.DropColumn(
                name: "status",
                table: "choice_groups");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "choice_groups");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "combo_group_items",
                newName: "product_variant_id");

            migrationBuilder.RenameColumn(
                name: "item_uom_id",
                table: "combo_group_items",
                newName: "product_id");

            migrationBuilder.RenameIndex(
                name: "IX_combo_group_items_item_uom_id",
                table: "combo_group_items",
                newName: "IX_combo_group_items_product_id");

            migrationBuilder.RenameColumn(
                name: "combo_name",
                table: "combo_definitions",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "choice_option_inventory_impacts",
                newName: "ingredient_product_id");

            migrationBuilder.RenameColumn(
                name: "group_code",
                table: "choice_groups",
                newName: "choice_group_code");

            migrationBuilder.RenameIndex(
                name: "uq_choice_groups_tenant_id_group_code",
                table: "choice_groups",
                newName: "uq_choice_groups_tenant_id_choice_group_code");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_choice_options",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "min_select",
                table: "combo_groups",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "combo_groups",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "combo_group_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "combo_definitions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "component_variant_id",
                table: "combo_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "choice_options",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "choice_options",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_delta",
                table: "choice_option_inventory_impacts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "min_select",
                table: "choice_groups",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "choice_groups",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_options_product_choice_group_id_choice_option_id",
                table: "product_choice_options",
                columns: new[] { "product_choice_group_id", "choice_option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_groups_product_id_choice_group_id",
                table: "product_choice_groups",
                columns: new[] { "product_id", "choice_group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_combo_groups_combo_definition_id_group_code",
                table: "combo_groups",
                columns: new[] { "combo_definition_id", "group_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_combo_group_items_combo_group_id_product_id_product_variant_id",
                table: "combo_group_items",
                columns: new[] { "combo_group_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_combo_definitions_tenant_id_product_id_combo_code",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "product_id", "combo_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_combo_components_combo_definition_id_component_product_id_component_variant_id",
                table: "combo_components",
                columns: new[] { "combo_definition_id", "component_product_id", "component_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_choice_options_choice_group_id_option_code",
                table: "choice_options",
                columns: new[] { "choice_group_id", "option_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_ingredient_product_id",
                table: "choice_option_inventory_impacts",
                column: "ingredient_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts",
                column: "product_choice_option_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_choice_option_inventory_impacts_quantity_delta",
                table: "choice_option_inventory_impacts",
                sql: "quantity_delta <> 0");

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_ingredient_product_id_products",
                table: "choice_option_inventory_impacts",
                column: "ingredient_product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id_product_choice_options",
                table: "choice_option_inventory_impacts",
                column: "product_choice_option_id",
                principalTable: "product_choice_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_product_id_products",
                table: "combo_group_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
