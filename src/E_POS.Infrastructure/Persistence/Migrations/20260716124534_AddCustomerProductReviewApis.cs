using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerProductReviewApis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_rating_summaries_product_id",
                table: "product_rating_summaries");

            migrationBuilder.AlterColumn<string>(
                name: "review_title",
                table: "product_reviews",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "review_text",
                table: "product_reviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ux_product_reviews_tenant_product_customer",
                table: "product_reviews",
                columns: new[] { "tenant_id", "product_id", "customer_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_reviews_rating_value",
                table: "product_reviews",
                sql: "rating_value BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_reviews_status",
                table: "product_reviews",
                sql: "status IN ('PENDING', 'APPROVED', 'REJECTED', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_rating_summaries_product_id",
                table: "product_rating_summaries",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_rating_summaries_tenant_product",
                table: "product_rating_summaries",
                columns: new[] { "tenant_id", "product_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_rating_summaries_average",
                table: "product_rating_summaries",
                sql: "average_rating BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_rating_summaries_counts",
                table: "product_rating_summaries",
                sql: "total_reviews >= 0 AND five_star_count >= 0 AND four_star_count >= 0 AND three_star_count >= 0 AND two_star_count >= 0 AND one_star_count >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_product_reviews_tenant_product_customer",
                table: "product_reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_reviews_rating_value",
                table: "product_reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_reviews_status",
                table: "product_reviews");

            migrationBuilder.DropIndex(
                name: "IX_product_rating_summaries_product_id",
                table: "product_rating_summaries");

            migrationBuilder.DropIndex(
                name: "ux_product_rating_summaries_tenant_product",
                table: "product_rating_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_rating_summaries_average",
                table: "product_rating_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_rating_summaries_counts",
                table: "product_rating_summaries");

            migrationBuilder.AlterColumn<string>(
                name: "review_title",
                table: "product_reviews",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "review_text",
                table: "product_reviews",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_rating_summaries_product_id",
                table: "product_rating_summaries",
                column: "product_id",
                unique: true);
        }
    }
}
