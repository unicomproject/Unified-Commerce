using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateProductWizardTo7Steps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE products SET current_setup_step = 7 WHERE current_setup_step = 8;");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_setup_step",
                table: "products");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_setup_step",
                table: "products",
                sql: "current_setup_step BETWEEN 1 AND 7");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_setup_step",
                table: "products");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_setup_step",
                table: "products",
                sql: "current_setup_step BETWEEN 1 AND 8");
        }
    }
}
