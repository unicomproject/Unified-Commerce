using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule13CombosWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_product_id_products",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_variant_id_variants",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_options_choice_group_id_choice_groups",
                table: "choice_options");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_combo_definition_id_combo_definitions",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_component_product_id_products",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_component_variant_id_product_variants",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_definitions_product_id_products",
                table: "combo_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_definitions_product_variant_id_product_variants",
                table: "combo_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_combo_group_id_combo_groups",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_product_id_products",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_variant_id_product_variants",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_groups_combo_definition_id_combo_definitions",
                table: "combo_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_choice_group_id_choice_groups",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_product_id_products",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_product_variant_id_product_variants",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_choice_group_id_choice_groups",
                table: "product_choice_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_choice_option_id_choice_options",
                table: "product_choice_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_product_choice_group_id_product_choice_groups",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_choice_group_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_choice_option_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_product_choice_group_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_groups_choice_group_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_groups_product_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_groups_product_variant_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "IX_combo_groups_combo_definition_id",
                table: "combo_groups");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_item_product_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_item_variant_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "IX_combo_definitions_product_id",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "IX_combo_definitions_product_variant_id",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_component_product_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_component_variant_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "IX_choice_options_choice_group_id",
                table: "choice_options");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_impact_product_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_impact_variant_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_choice_options_tenant_id_id",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_choice_groups_tenant_id_id",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_combo_groups_tenant_id_id",
                table: "combo_groups",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_combo_definitions_tenant_id_id",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_choice_options_tenant_id_id",
                table: "choice_options",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_choice_groups_tenant_id_id",
                table: "choice_groups",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_tenant_id_choice_group_id",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "choice_group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_tenant_id_choice_option_id",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "choice_option_id" });

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_options_tenant_id_id",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_tenant_id_choice_group_id",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "choice_group_id" });

            migrationBuilder.CreateIndex(
                name: "uq_product_choice_groups_tenant_id_id",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_combo_groups_tenant_id_id",
                table: "combo_groups",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_tenant_id_combo_group_id",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "combo_group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_tenant_id_item_product_id",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "item_product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_tenant_id_item_variant_id",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "item_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_combo_definitions_tenant_id_id",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_tenant_id_combo_definition_id",
                table: "combo_components",
                columns: new[] { "tenant_id", "combo_definition_id" });

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_tenant_id_component_product_id",
                table: "combo_components",
                columns: new[] { "tenant_id", "component_product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_tenant_id_component_variant_id",
                table: "combo_components",
                columns: new[] { "tenant_id", "component_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_choice_options_tenant_id_id",
                table: "choice_options",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_tenant_id_impact_product_id",
                table: "choice_option_inventory_impacts",
                columns: new[] { "tenant_id", "impact_product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_tenant_id_impact_variant_id",
                table: "choice_option_inventory_impacts",
                columns: new[] { "tenant_id", "impact_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_tenant_id_product_choice_op~",
                table: "choice_option_inventory_impacts",
                columns: new[] { "tenant_id", "product_choice_option_id" });

            migrationBuilder.CreateIndex(
                name: "uq_choice_groups_tenant_id_id",
                table: "choice_groups",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_product_id_products",
                table: "choice_option_inventory_impacts",
                columns: new[] { "tenant_id", "impact_product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_variant_id_variants",
                table: "choice_option_inventory_impacts",
                columns: new[] { "tenant_id", "impact_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts",
                columns: new[] { "tenant_id", "product_choice_option_id" },
                principalTable: "product_choice_options",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_choice_options_choice_group_id_choice_groups",
                table: "choice_options",
                columns: new[] { "tenant_id", "choice_group_id" },
                principalTable: "choice_groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_combo_definition_id_combo_definitions",
                table: "combo_components",
                columns: new[] { "tenant_id", "combo_definition_id" },
                principalTable: "combo_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_component_product_id_products",
                table: "combo_components",
                columns: new[] { "tenant_id", "component_product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_component_variant_id_product_variants",
                table: "combo_components",
                columns: new[] { "tenant_id", "component_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_definitions_product_id_products",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_definitions_product_variant_id_product_variants",
                table: "combo_definitions",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_combo_group_id_combo_groups",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "combo_group_id" },
                principalTable: "combo_groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_item_product_id_products",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "item_product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_group_items_item_variant_id_product_variants",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "item_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_groups_combo_definition_id_combo_definitions",
                table: "combo_groups",
                columns: new[] { "tenant_id", "combo_definition_id" },
                principalTable: "combo_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_groups_choice_group_id_choice_groups",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "choice_group_id" },
                principalTable: "choice_groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_groups_product_id_products",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_groups_product_variant_id_product_variants",
                table: "product_choice_groups",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_options_choice_group_id_choice_groups",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "choice_group_id" },
                principalTable: "choice_groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_options_choice_option_id_choice_options",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "choice_option_id" },
                principalTable: "choice_options",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_options_product_choice_group_id_product_choice_groups",
                table: "product_choice_options",
                columns: new[] { "tenant_id", "product_choice_group_id" },
                principalTable: "product_choice_groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_product_id_products",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_variant_id_variants",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_option_inventory_impacts_product_choice_option_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropForeignKey(
                name: "fk_choice_options_choice_group_id_choice_groups",
                table: "choice_options");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_combo_definition_id_combo_definitions",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_component_product_id_products",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_components_component_variant_id_product_variants",
                table: "combo_components");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_definitions_product_id_products",
                table: "combo_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_definitions_product_variant_id_product_variants",
                table: "combo_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_combo_group_id_combo_groups",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_product_id_products",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_group_items_item_variant_id_product_variants",
                table: "combo_group_items");

            migrationBuilder.DropForeignKey(
                name: "fk_combo_groups_combo_definition_id_combo_definitions",
                table: "combo_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_choice_group_id_choice_groups",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_product_id_products",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_groups_product_variant_id_product_variants",
                table: "product_choice_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_choice_group_id_choice_groups",
                table: "product_choice_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_choice_option_id_choice_options",
                table: "product_choice_options");

            migrationBuilder.DropForeignKey(
                name: "fk_product_choice_options_product_choice_group_id_product_choice_groups",
                table: "product_choice_options");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_choice_options_tenant_id_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_tenant_id_choice_group_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_options_tenant_id_choice_option_id",
                table: "product_choice_options");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_options_tenant_id_id",
                table: "product_choice_options");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_choice_groups_tenant_id_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "IX_product_choice_groups_tenant_id_choice_group_id",
                table: "product_choice_groups");

            migrationBuilder.DropIndex(
                name: "uq_product_choice_groups_tenant_id_id",
                table: "product_choice_groups");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_combo_groups_tenant_id_id",
                table: "combo_groups");

            migrationBuilder.DropIndex(
                name: "uq_combo_groups_tenant_id_id",
                table: "combo_groups");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_tenant_id_combo_group_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_tenant_id_item_product_id",
                table: "combo_group_items");

            migrationBuilder.DropIndex(
                name: "IX_combo_group_items_tenant_id_item_variant_id",
                table: "combo_group_items");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_combo_definitions_tenant_id_id",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "uq_combo_definitions_tenant_id_id",
                table: "combo_definitions");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_tenant_id_combo_definition_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_tenant_id_component_product_id",
                table: "combo_components");

            migrationBuilder.DropIndex(
                name: "IX_combo_components_tenant_id_component_variant_id",
                table: "combo_components");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_choice_options_tenant_id_id",
                table: "choice_options");

            migrationBuilder.DropIndex(
                name: "uq_choice_options_tenant_id_id",
                table: "choice_options");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_tenant_id_impact_product_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_tenant_id_impact_variant_id",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropIndex(
                name: "IX_choice_option_inventory_impacts_tenant_id_product_choice_op~",
                table: "choice_option_inventory_impacts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_choice_groups_tenant_id_id",
                table: "choice_groups");

            migrationBuilder.DropIndex(
                name: "uq_choice_groups_tenant_id_id",
                table: "choice_groups");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_choice_group_id",
                table: "product_choice_options",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_choice_option_id",
                table: "product_choice_options",
                column: "choice_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_options_product_choice_group_id",
                table: "product_choice_options",
                column: "product_choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_choice_group_id",
                table: "product_choice_groups",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_product_id",
                table: "product_choice_groups",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_choice_groups_product_variant_id",
                table: "product_choice_groups",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_groups_combo_definition_id",
                table: "combo_groups",
                column: "combo_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_item_product_id",
                table: "combo_group_items",
                column: "item_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_group_items_item_variant_id",
                table: "combo_group_items",
                column: "item_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_definitions_product_id",
                table: "combo_definitions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_definitions_product_variant_id",
                table: "combo_definitions",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_component_product_id",
                table: "combo_components",
                column: "component_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_components_component_variant_id",
                table: "combo_components",
                column: "component_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_options_choice_group_id",
                table: "choice_options",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_impact_product_id",
                table: "choice_option_inventory_impacts",
                column: "impact_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_choice_option_inventory_impacts_impact_variant_id",
                table: "choice_option_inventory_impacts",
                column: "impact_variant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_choice_option_inventory_impacts_impact_product_id_products",
                table: "choice_option_inventory_impacts",
                column: "impact_product_id",
                principalTable: "products",
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
                name: "fk_choice_options_choice_group_id_choice_groups",
                table: "choice_options",
                column: "choice_group_id",
                principalTable: "choice_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_combo_definition_id_combo_definitions",
                table: "combo_components",
                column: "combo_definition_id",
                principalTable: "combo_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_components_component_product_id_products",
                table: "combo_components",
                column: "component_product_id",
                principalTable: "products",
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
                name: "fk_combo_definitions_product_id_products",
                table: "combo_definitions",
                column: "product_id",
                principalTable: "products",
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
                name: "fk_combo_group_items_combo_group_id_combo_groups",
                table: "combo_group_items",
                column: "combo_group_id",
                principalTable: "combo_groups",
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
                name: "fk_combo_group_items_item_variant_id_product_variants",
                table: "combo_group_items",
                column: "item_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_combo_groups_combo_definition_id_combo_definitions",
                table: "combo_groups",
                column: "combo_definition_id",
                principalTable: "combo_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_groups_choice_group_id_choice_groups",
                table: "product_choice_groups",
                column: "choice_group_id",
                principalTable: "choice_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_groups_product_id_products",
                table: "product_choice_groups",
                column: "product_id",
                principalTable: "products",
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

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_options_choice_option_id_choice_options",
                table: "product_choice_options",
                column: "choice_option_id",
                principalTable: "choice_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_choice_options_product_choice_group_id_product_choice_groups",
                table: "product_choice_options",
                column: "product_choice_group_id",
                principalTable: "product_choice_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
