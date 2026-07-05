using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule12ProductOptionsWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_option_values_product_option_id_product_options",
                table: "product_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_id_products",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_option_id_product_options",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_option_value_id_product_option_values",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_variant_id_product_variants",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_option_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_option_value_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_product_variant_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_option_values_product_option_id",
                table: "product_option_values");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_options_tenant_id_id",
                table: "product_options",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_option_values_tenant_id_id",
                table: "product_option_values",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_tenant_id_product_id",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_tenant_id_product_option_id",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_option_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_tenant_id_product_option_valu~",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_option_value_id" });

            migrationBuilder.CreateIndex(
                name: "uq_product_options_tenant_id_id",
                table: "product_options",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_options_tenant_id_product_id_id",
                table: "product_options",
                columns: new[] { "tenant_id", "product_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_values_tenant_id_id",
                table: "product_option_values",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_values_tenant_id_product_option_id_id",
                table: "product_option_values",
                columns: new[] { "tenant_id", "product_option_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_option_template_values_option_template_id_id",
                table: "product_option_template_values",
                columns: new[] { "option_template_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_values_product_option_id_product_options",
                table: "product_option_values",
                columns: new[] { "tenant_id", "product_option_id" },
                principalTable: "product_options",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_id_products",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_option_id_product_options",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_option_id" },
                principalTable: "product_options",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_option_value_id_product_option_values",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_option_value_id" },
                principalTable: "product_option_values",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_variant_id_product_variants",
                table: "product_variant_option_values",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_option_values_product_option_id_product_options",
                table: "product_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_id_products",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_option_id_product_options",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_option_value_id_product_option_values",
                table: "product_variant_option_values");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variant_option_values_product_variant_id_product_variants",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_tenant_id_product_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_tenant_id_product_option_id",
                table: "product_variant_option_values");

            migrationBuilder.DropIndex(
                name: "IX_product_variant_option_values_tenant_id_product_option_valu~",
                table: "product_variant_option_values");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_options_tenant_id_id",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "uq_product_options_tenant_id_id",
                table: "product_options");

            migrationBuilder.DropIndex(
                name: "uq_product_options_tenant_id_product_id_id",
                table: "product_options");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_option_values_tenant_id_id",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_option_values_tenant_id_id",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_option_values_tenant_id_product_option_id_id",
                table: "product_option_values");

            migrationBuilder.DropIndex(
                name: "uq_product_option_template_values_option_template_id_id",
                table: "product_option_template_values");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_id",
                table: "product_variant_option_values",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_option_id",
                table: "product_variant_option_values",
                column: "product_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_option_value_id",
                table: "product_variant_option_values",
                column: "product_option_value_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_option_values_product_variant_id",
                table: "product_variant_option_values",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_option_values_product_option_id",
                table: "product_option_values",
                column: "product_option_id");

            migrationBuilder.AddForeignKey(
                name: "fk_product_option_values_product_option_id_product_options",
                table: "product_option_values",
                column: "product_option_id",
                principalTable: "product_options",
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
                name: "fk_product_variant_option_values_product_option_value_id_product_option_values",
                table: "product_variant_option_values",
                column: "product_option_value_id",
                principalTable: "product_option_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variant_option_values_product_variant_id_product_variants",
                table: "product_variant_option_values",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
