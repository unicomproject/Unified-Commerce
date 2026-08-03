using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardwareChunk2CReprintCopies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_receipt_print_logs_reprint_operation_id",
                table: "receipt_print_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_print_logs_printed_copy_type",
                table: "receipt_print_logs");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_print_logs_reprint_operation_id",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "reprint_operation_id" },
                filter: "reprint_operation_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_print_logs_printed_copy_type",
                table: "receipt_print_logs",
                sql: "printed_copy_type IN ('CUSTOMER_COPY', 'MERCHANT_COPY', 'DUPLICATE_COPY', 'DUPLICATE_CUSTOMER_COPY', 'DUPLICATE_MERCHANT_COPY')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_receipt_print_logs_reprint_operation_id",
                table: "receipt_print_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_print_logs_printed_copy_type",
                table: "receipt_print_logs");

            migrationBuilder.Sql(
                """
                UPDATE receipt_print_logs
                SET reprint_operation_id = NULL,
                    printed_copy_type = 'DUPLICATE_COPY'
                WHERE printed_copy_type IN (
                    'DUPLICATE_CUSTOMER_COPY',
                    'DUPLICATE_MERCHANT_COPY'
                );
                """);

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_reprint_operation_id",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "reprint_operation_id" },
                unique: true,
                filter: "reprint_operation_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_print_logs_printed_copy_type",
                table: "receipt_print_logs",
                sql: "printed_copy_type IN ('CUSTOMER_COPY', 'MERCHANT_COPY', 'DUPLICATE_COPY')");
        }
    }
}
