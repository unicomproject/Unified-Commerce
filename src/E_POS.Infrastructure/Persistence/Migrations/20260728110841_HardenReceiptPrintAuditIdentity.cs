using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenReceiptPrintAuditIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_correlation_id",
                table: "receipt_print_logs",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "print_request_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reprint_operation_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipt_print_logs_client_correlation_id",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "client_correlation_id" },
                filter: "client_correlation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_print_logs_print_status",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "print_status" });

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_print_request_id",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "receipt_id", "print_request_id" },
                unique: true,
                filter: "print_request_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_reprint_operation_id",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "reprint_operation_id" },
                unique: true,
                filter: "reprint_operation_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_receipt_print_logs_client_correlation_id",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "ix_receipt_print_logs_print_status",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "uq_receipt_print_logs_print_request_id",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "uq_receipt_print_logs_reprint_operation_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "client_correlation_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "print_request_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "reprint_operation_id",
                table: "receipt_print_logs");
        }
    }
}
