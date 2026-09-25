using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPaymentConcurrencyAndWebhookDedup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "sales_payments",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "payment_provider_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    external_event_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_provider_webhook_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_payment_provider_webhook_events_provider_event_id",
                table: "payment_provider_webhook_events",
                columns: new[] { "provider", "external_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_provider_webhook_events");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "sales_payments");
        }
    }
}
