using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupOrderFailedVerificationAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_verification_attempts",
                table: "pickup_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_orders_failed_verification_attempts",
                table: "pickup_orders",
                sql: "failed_verification_attempts >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_orders_failed_verification_attempts",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "failed_verification_attempts",
                table: "pickup_orders");
        }
    }
}
