using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardwareChunk2ReceiptPrintAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent_result",
                table: "receipt_print_logs",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "receipt_print_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "copy_index",
                table: "receipt_print_logs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "failure_category",
                table: "receipt_print_logs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_reprint",
                table: "receipt_print_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "pos_device_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "printer_configuration_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "printer_configuration_version",
                table: "receipt_print_logs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "printer_name",
                table: "receipt_print_logs",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "printer_transport",
                table: "receipt_print_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receipt_purpose",
                table: "receipt_print_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "SALE_ORIGINAL");

            migrationBuilder.AddColumn<Guid>(
                name: "recovery_print_request_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "routing_purpose",
                table: "receipt_print_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_session_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "unknown_outcome",
                table: "receipt_print_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_receipt_print_logs_printer_configuration",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "printer_configuration_id", "printer_configuration_version" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_print_logs_copy_index",
                table: "receipt_print_logs",
                sql: "copy_index BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_print_logs_receipt_purpose",
                table: "receipt_print_logs",
                sql: "receipt_purpose IN ('SALE_ORIGINAL', 'SALE_REPRINT', 'RETURN', 'EXCHANGE', 'REFUND')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_receipt_print_logs_printer_configuration",
                table: "receipt_print_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_print_logs_copy_index",
                table: "receipt_print_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_print_logs_receipt_purpose",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "agent_result",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "copy_index",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "failure_category",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "is_reprint",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "pos_device_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printer_configuration_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printer_configuration_version",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printer_name",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printer_transport",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "receipt_purpose",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "recovery_print_request_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "routing_purpose",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "till_session_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "unknown_outcome",
                table: "receipt_print_logs");
        }
    }
}
