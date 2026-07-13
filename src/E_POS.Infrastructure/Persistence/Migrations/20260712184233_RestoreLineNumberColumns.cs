using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreLineNumberColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_items_shopping_cart_id",
                table: "shopping_cart_items");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_lines_checkout_session_id",
                table: "checkout_session_lines");

            migrationBuilder.AddColumn<int>(
                name: "line_number",
                table: "shopping_cart_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "line_number",
                table: "checkout_session_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "uq_shopping_cart_items_shopping_cart_id_line_number",
                table: "shopping_cart_items",
                columns: new[] { "shopping_cart_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_line_number",
                table: "shopping_cart_items",
                sql: "line_number > 0");

            migrationBuilder.CreateIndex(
                name: "uq_checkout_session_lines_checkout_session_id_line_number",
                table: "checkout_session_lines",
                columns: new[] { "checkout_session_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_lines_line_number",
                table: "checkout_session_lines",
                sql: "line_number > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_shopping_cart_items_shopping_cart_id_line_number",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_line_number",
                table: "shopping_cart_items");

            migrationBuilder.DropIndex(
                name: "uq_checkout_session_lines_checkout_session_id_line_number",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_lines_line_number",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "checkout_session_lines");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_items_shopping_cart_id",
                table: "shopping_cart_items",
                column: "shopping_cart_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_lines_checkout_session_id",
                table: "checkout_session_lines",
                column: "checkout_session_id");
        }
    }
}
