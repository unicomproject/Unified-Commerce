using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentRefundRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_sales_payments_3aae300c",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_transactions_5562416e",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_transactions_e759526b",
                table: "sales_payment_transactions");

            migrationBuilder.AlterColumn<string>(
                name: "refund_status",
                table: "sales_refunds",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "refund_mode",
                table: "sales_refunds",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "allocation_status",
                table: "sales_refund_payment_allocations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "refund_line_type",
                table: "sales_refund_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "payment_status",
                table: "sales_payments",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "sales_payment_transactions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_status",
                table: "sales_payment_transactions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "old_status",
                table: "sales_payment_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "new_status",
                table: "sales_payment_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                table: "sales_payment_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payment_methods",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "method_type",
                table: "payment_methods",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_app_amt",
                table: "sales_refunds",
                sql: "approved_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_mode",
                table: "sales_refunds",
                sql: "refund_mode IN ('ORIGINAL_PAYMENT', 'CASH', 'MIXED', 'MANUAL')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_ref_amt",
                table: "sales_refunds",
                sql: "refunded_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_status",
                table: "sales_refunds",
                sql: "refund_status IN ('REQUESTED', 'APPROVED', 'PROCESSING', 'COMPLETED', 'CANCELLED', 'FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_payment_allocations_status",
                table: "sales_refund_payment_allocations",
                sql: "allocation_status IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_type",
                table: "sales_refund_lines",
                sql: "refund_line_type IN ('ITEM', 'TAX', 'CHARGE', 'ROUNDING', 'GENERAL')");

            migrationBuilder.CreateIndex(
                name: "ux_sales_payments_3aae300c",
                table: "sales_payments",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_change_amt",
                table: "sales_payments",
                sql: "change_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_paid_amt",
                table: "sales_payments",
                sql: "paid_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_ref_amt",
                table: "sales_payments",
                sql: "refunded_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_status",
                table: "sales_payments",
                sql: "payment_status IN ('PENDING', 'AUTHORIZED', 'PAID', 'PARTIALLY_REFUNDED', 'REFUNDED', 'FAILED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "ux_sales_payment_transactions_5562416e",
                table: "sales_payment_transactions",
                columns: new[] { "tenant_id", "provider_name", "external_transaction_reference" },
                unique: true,
                filter: "external_transaction_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_sales_payment_transactions_e759526b",
                table: "sales_payment_transactions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_transactions_status",
                table: "sales_payment_transactions",
                sql: "transaction_status IN ('PENDING', 'SUCCEEDED', 'FAILED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_transactions_type",
                table: "sales_payment_transactions",
                sql: "transaction_type IN ('AUTHORIZE', 'CAPTURE', 'SALE', 'REFUND', 'VOID', 'REVERSAL')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_method_type",
                table: "payment_methods",
                sql: "method_type IN ('CASH', 'CARD', 'QR', 'BANK_TRANSFER', 'MANUAL', 'OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_status",
                table: "payment_methods",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_app_amt",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_mode",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_ref_amt",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_status",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_payment_allocations_status",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_type",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "ux_sales_payments_3aae300c",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_change_amt",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_paid_amt",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_ref_amt",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_status",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_transactions_5562416e",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_transactions_e759526b",
                table: "sales_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_transactions_status",
                table: "sales_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_transactions_type",
                table: "sales_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_method_type",
                table: "payment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_status",
                table: "payment_methods");

            migrationBuilder.AlterColumn<string>(
                name: "refund_status",
                table: "sales_refunds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "refund_mode",
                table: "sales_refunds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "allocation_status",
                table: "sales_refund_payment_allocations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "refund_line_type",
                table: "sales_refund_lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "payment_status",
                table: "sales_payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "sales_payment_transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "transaction_status",
                table: "sales_payment_transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "old_status",
                table: "sales_payment_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "new_status",
                table: "sales_payment_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                table: "sales_payment_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payment_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "method_type",
                table: "payment_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateIndex(
                name: "ux_sales_payments_3aae300c",
                table: "sales_payments",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sales_payment_transactions_5562416e",
                table: "sales_payment_transactions",
                columns: new[] { "tenant_id", "provider_name", "external_transaction_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sales_payment_transactions_e759526b",
                table: "sales_payment_transactions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);
        }
    }
}
