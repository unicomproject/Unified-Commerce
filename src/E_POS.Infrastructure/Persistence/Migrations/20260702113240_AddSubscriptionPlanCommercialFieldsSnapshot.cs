using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanCommercialFieldsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "base_currency",
                table: "subscription_plans",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LKR");

            migrationBuilder.AddColumn<int>(
                name: "max_outlets",
                table: "subscription_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_tills",
                table: "subscription_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_users",
                table: "subscription_plans",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_currency",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "max_outlets",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "max_tills",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "max_users",
                table: "subscription_plans");
        }
    }
}
