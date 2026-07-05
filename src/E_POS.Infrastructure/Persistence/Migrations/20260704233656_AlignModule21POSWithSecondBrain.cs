using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule21POSWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pos_order_holds_sales_order_id_sales_orders",
                table: "pos_order_holds");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_print_logs_receipt_id_receipts",
                table: "receipt_print_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_versions_receipt_template_id_receipt_templates",
                table: "receipt_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_templates_parent_template_id_receipt_templates",
                table: "receipt_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_document_number_sequence_id_document_number_sequences",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_sales_order_id_sales_orders",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_till_cash_movements_till_session_id_till_sessions",
                table: "till_cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_events_till_session_id_till_sessions",
                table: "till_session_events");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_payment_summaries_payment_method_id_payment_methods",
                table: "till_session_payment_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries",
                table: "till_session_payment_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_till_session_id_till_sessions",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_till_id_tills",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_till_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "uq_till_session_summaries_till_session_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_payment_summaries_payment_method_id",
                table: "till_session_payment_summaries");

            migrationBuilder.DropIndex(
                name: "uq_till_session_payment_summaries_till_session_summary_id_payment_method_id",
                table: "till_session_payment_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_events_till_session_id",
                table: "till_session_events");

            migrationBuilder.DropIndex(
                name: "IX_till_cash_movements_till_session_id",
                table: "till_cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_receipts_document_number_sequence_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_sales_order_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipt_templates_parent_template_id",
                table: "receipt_templates");

            migrationBuilder.DropIndex(
                name: "uq_receipt_template_versions_receipt_template_id_version_number",
                table: "receipt_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_receipt_template_version_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_template_assignments_assignment_scope_outlet_id_til~",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "uq_receipt_print_logs_receipt_id_attempt_number",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "IX_pos_order_holds_sales_order_id",
                table: "pos_order_holds");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "till_session_summaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_tenant_user_id",
                table: "till_session_summaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cash_difference_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "cashier_tenant_user_id",
                table: "till_session_summaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "charge_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "counted_cash_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "expected_cash_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "generated_at",
                table: "till_session_summaries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "gross_sales_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "net_sales_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "opening_cash_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "till_session_summaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "session_closed_at",
                table: "till_session_summaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "session_opened_at",
                table: "till_session_summaries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "summary_status",
                table: "till_session_summaries",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "till_session_summaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "till_session_summaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "void_amount",
                table: "till_session_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "void_count",
                table: "till_session_summaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "net_amount",
                table: "till_session_payment_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "till_session_payment_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sales_amount",
                table: "till_session_payment_summaries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "till_session_payment_summaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "transaction_count",
                table: "till_session_payment_summaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "till_session_events",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "till_session_events",
                type: "char(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "event_at",
                table: "till_session_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "event_by_tenant_user_id",
                table: "till_session_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_payload_json",
                table: "till_session_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "till_session_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<IPAddress>(
                name: "ip_address",
                table: "till_session_events",
                type: "inet",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "till_session_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pos_device_id",
                table: "till_session_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reference_id",
                table: "till_session_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_type",
                table: "till_session_events",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "till_session_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "till_cash_movements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "till_cash_movements",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "movement_type",
                table: "till_cash_movements",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "performed_at",
                table: "till_cash_movements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "performed_by_tenant_user_id",
                table: "till_cash_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "till_cash_movements",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "till_cash_movements",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "till_cash_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "document_number_sequence_id",
                table: "receipts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateOnly>(
                name: "business_date",
                table: "receipts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "change_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "charge_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "receipts",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "issued_at",
                table: "receipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "issued_by_tenant_user_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "receipt_data_json",
                table: "receipts",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "receipt_status",
                table: "receipts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "receipt_template_version_id",
                table: "receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receipt_type",
                table: "receipts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "rounding_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "till_session_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "receipt_templates",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<bool>(
                name: "is_base_template",
                table: "receipt_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "template_type",
                table: "receipt_templates",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from",
                table: "receipt_template_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_to",
                table: "receipt_template_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "receipt_template_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "page_size",
                table: "receipt_template_versions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_data",
                table: "receipt_template_versions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "receipt_template_versions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "till_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "pos_device_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from",
                table: "receipt_template_assignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_to",
                table: "receipt_template_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "receipt_template_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "receipt_template_assignments",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "receipt_print_logs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "receipt_print_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "operator_tenant_user_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "print_result_json",
                table: "receipt_print_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "print_status",
                table: "receipt_print_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "printed_at",
                table: "receipt_print_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "printed_copy_type",
                table: "receipt_print_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "printer_device_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "receipt_print_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "pos_order_holds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "pos_order_holds",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "pos_order_holds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "pos_order_holds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "held_at",
                table: "pos_order_holds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "held_by_tenant_user_id",
                table: "pos_order_holds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "hold_reason",
                table: "pos_order_holds",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hold_status",
                table: "pos_order_holds",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "released_at",
                table: "pos_order_holds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "released_by_tenant_user_id",
                table: "pos_order_holds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tills_tenant_id_id",
                table: "tills",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_till_sessions_tenant_id_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_till_session_summaries_tenant_id_id",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_sales_orders_tenant_id_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_receipts_tenant_id_id",
                table: "receipts",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_receipt_templates_tenant_id_id",
                table: "receipt_templates",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_receipt_template_versions_tenant_id_id",
                table: "receipt_template_versions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_pos_devices_tenant_id_id",
                table: "pos_devices",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_payment_methods_tenant_id_id",
                table: "payment_methods",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_document_number_sequences_tenant_id_id",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "uq_tills_tenant_id_id",
                table: "tills",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_sessions_tenant_id_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_till_session_summaries_approved_by_tenant_user_id",
                table: "till_session_summaries",
                column: "approved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_session_summaries_cashier_tenant_user_id",
                table: "till_session_summaries",
                column: "cashier_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_session_summaries_tenant_id_outlet_id",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_till_session_summaries_tenant_id_till_id",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "uq_till_session_summaries_tenant_id_id",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_session_summaries_till_session_id",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "till_session_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_charge_amount",
                table: "till_session_summaries",
                sql: "charge_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_counted_cash_amount",
                table: "till_session_summaries",
                sql: "counted_cash_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_discount_amount",
                table: "till_session_summaries",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_expected_cash_amount",
                table: "till_session_summaries",
                sql: "expected_cash_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_gross_sales_amount",
                table: "till_session_summaries",
                sql: "gross_sales_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_net_sales_amount",
                table: "till_session_summaries",
                sql: "net_sales_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_opening_cash_amount",
                table: "till_session_summaries",
                sql: "opening_cash_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_refund_amount",
                table: "till_session_summaries",
                sql: "refund_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_summary_status",
                table: "till_session_summaries",
                sql: "summary_status IN ('GENERATED', 'APPROVED', 'REJECTED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_tax_amount",
                table: "till_session_summaries",
                sql: "tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_void_amount",
                table: "till_session_summaries",
                sql: "void_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_summaries_void_count",
                table: "till_session_summaries",
                sql: "void_count >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_till_session_payment_summaries_tenant_id_payment_method_id",
                table: "till_session_payment_summaries",
                columns: new[] { "tenant_id", "payment_method_id" });

            migrationBuilder.CreateIndex(
                name: "uq_till_session_payment_summaries_tenant_id_id",
                table: "till_session_payment_summaries",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_session_payment_summaries_till_session_summary_id_payment_method_id",
                table: "till_session_payment_summaries",
                columns: new[] { "tenant_id", "till_session_summary_id", "payment_method_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_payment_summaries_refund_amount",
                table: "till_session_payment_summaries",
                sql: "refund_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_payment_summaries_sales_amount",
                table: "till_session_payment_summaries",
                sql: "sales_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_payment_summaries_transaction_count",
                table: "till_session_payment_summaries",
                sql: "transaction_count >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_till_session_events_event_by_tenant_user_id",
                table: "till_session_events",
                column: "event_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_session_events_tenant_id_pos_device_id",
                table: "till_session_events",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_till_session_events_tenant_id_till_session_id",
                table: "till_session_events",
                columns: new[] { "tenant_id", "till_session_id" });

            migrationBuilder.CreateIndex(
                name: "uq_till_session_events_tenant_id_id",
                table: "till_session_events",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_session_events_event_type",
                table: "till_session_events",
                sql: "event_type IN ('OPENED', 'CLOSED', 'PAUSED', 'RESUMED', 'CASH_IN', 'CASH_OUT', 'NOTE')");

            migrationBuilder.CreateIndex(
                name: "IX_till_cash_movements_performed_by_tenant_user_id",
                table: "till_cash_movements",
                column: "performed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_cash_movements_tenant_id_till_session_id",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "till_session_id" });

            migrationBuilder.CreateIndex(
                name: "uq_till_cash_movements_tenant_id_id",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_cash_movements_movement_type",
                table: "till_cash_movements",
                sql: "movement_type IN ('CASH_IN', 'CASH_OUT', 'OPENING_FLOAT', 'CLOSING_REMOVE')");

            migrationBuilder.CreateIndex(
                name: "uq_sales_orders_tenant_id_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipts_issued_by_tenant_user_id",
                table: "receipts",
                column: "issued_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_tenant_id_document_number_sequence_id",
                table: "receipts",
                columns: new[] { "tenant_id", "document_number_sequence_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_tenant_id_outlet_id",
                table: "receipts",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_tenant_id_receipt_template_version_id",
                table: "receipts",
                columns: new[] { "tenant_id", "receipt_template_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_tenant_id_sales_order_id",
                table: "receipts",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_tenant_id_till_id",
                table: "receipts",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_tenant_id_till_session_id",
                table: "receipts",
                columns: new[] { "tenant_id", "till_session_id" });

            migrationBuilder.CreateIndex(
                name: "uq_receipts_tenant_id_id",
                table: "receipts",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_change_amount",
                table: "receipts",
                sql: "change_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_charge_amount",
                table: "receipts",
                sql: "charge_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_discount_amount",
                table: "receipts",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_paid_amount",
                table: "receipts",
                sql: "paid_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_receipt_status",
                table: "receipts",
                sql: "receipt_status IN ('ISSUED', 'VOIDED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_receipt_type",
                table: "receipts",
                sql: "receipt_type IN ('SALE', 'REFUND', 'EXCHANGE', 'REPRINT')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_subtotal_amount",
                table: "receipts",
                sql: "subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipts_tax_amount",
                table: "receipts",
                sql: "tax_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_templates_tenant_id_parent_template_id",
                table: "receipt_templates",
                columns: new[] { "tenant_id", "parent_template_id" });

            migrationBuilder.CreateIndex(
                name: "uq_receipt_templates_tenant_id_id",
                table: "receipt_templates",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_templates_status",
                table: "receipt_templates",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_templates_template_type",
                table: "receipt_templates",
                sql: "template_type IN ('POS_RECEIPT', 'REFUND_RECEIPT', 'EXCHANGE_RECEIPT')");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_template_versions_receipt_template_id_version_number",
                table: "receipt_template_versions",
                columns: new[] { "tenant_id", "receipt_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_receipt_template_versions_tenant_id_id",
                table: "receipt_template_versions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_template_versions_effective_dates",
                table: "receipt_template_versions",
                sql: "effective_to IS NULL OR effective_from IS NULL OR effective_to >= effective_from");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_tenant_id_outlet_id",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_tenant_id_pos_device_id",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_tenant_id_receipt_template_ver~",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "receipt_template_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_tenant_id_till_id",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "uq_receipt_template_assignments_tenant_id_id",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_template_assignments_assignment_scope",
                table: "receipt_template_assignments",
                sql: "assignment_scope IN ('OUTLET', 'TILL', 'POS_DEVICE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_template_assignments_effective_dates",
                table: "receipt_template_assignments",
                sql: "effective_to IS NULL OR effective_to >= effective_from");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_template_assignments_scope_rules",
                table: "receipt_template_assignments",
                sql: "(assignment_scope = 'OUTLET' AND outlet_id IS NOT NULL AND till_id IS NULL AND pos_device_id IS NULL) OR (assignment_scope = 'TILL' AND outlet_id IS NULL AND till_id IS NOT NULL AND pos_device_id IS NULL) OR (assignment_scope = 'POS_DEVICE' AND outlet_id IS NULL AND till_id IS NULL AND pos_device_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_template_assignments_status",
                table: "receipt_template_assignments",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_print_logs_operator_tenant_user_id",
                table: "receipt_print_logs",
                column: "operator_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_receipt_id_attempt_number",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "receipt_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_tenant_id_id",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_print_logs_print_status",
                table: "receipt_print_logs",
                sql: "print_status IN ('PENDING', 'PRINTED', 'FAILED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_print_logs_printed_copy_type",
                table: "receipt_print_logs",
                sql: "printed_copy_type IN ('CUSTOMER_COPY', 'MERCHANT_COPY', 'DUPLICATE_COPY')");

            migrationBuilder.CreateIndex(
                name: "IX_pos_order_holds_held_by_tenant_user_id",
                table: "pos_order_holds",
                column: "held_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_order_holds_released_by_tenant_user_id",
                table: "pos_order_holds",
                column: "released_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_order_holds_tenant_id_sales_order_id",
                table: "pos_order_holds",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "uq_pos_order_holds_tenant_id_id",
                table: "pos_order_holds",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pos_order_holds_hold_status",
                table: "pos_order_holds",
                sql: "hold_status IN ('HELD', 'RELEASED', 'EXPIRED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "uq_pos_devices_tenant_id_id",
                table: "pos_devices",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_methods_tenant_id_id",
                table: "payment_methods",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_document_number_sequences_tenant_id_id",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_order_holds_held_by_tenant_user_id_tenant_users",
                table: "pos_order_holds",
                column: "held_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_order_holds_released_by_tenant_user_id_tenant_users",
                table: "pos_order_holds",
                column: "released_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_order_holds_sales_order_id_sales_orders",
                table: "pos_order_holds",
                columns: new[] { "tenant_id", "sales_order_id" },
                principalTable: "sales_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_order_holds_tenant_id_tenants",
                table: "pos_order_holds",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_print_logs_operator_tenant_user_id_tenant_users",
                table: "receipt_print_logs",
                column: "operator_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_print_logs_receipt_id_receipts",
                table: "receipt_print_logs",
                columns: new[] { "tenant_id", "receipt_id" },
                principalTable: "receipts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_print_logs_tenant_id_tenants",
                table: "receipt_print_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_outlet_id_outlets",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_pos_device_id_pos_devices",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "receipt_template_version_id" },
                principalTable: "receipt_template_versions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_tenant_id_tenants",
                table: "receipt_template_assignments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_till_id_tills",
                table: "receipt_template_assignments",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_versions_receipt_template_id_receipt_templates",
                table: "receipt_template_versions",
                columns: new[] { "tenant_id", "receipt_template_id" },
                principalTable: "receipt_templates",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_versions_tenant_id_tenants",
                table: "receipt_template_versions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_templates_parent_template_id_receipt_templates",
                table: "receipt_templates",
                columns: new[] { "tenant_id", "parent_template_id" },
                principalTable: "receipt_templates",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_templates_tenant_id_tenants",
                table: "receipt_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_document_number_sequence_id_document_number_sequences",
                table: "receipts",
                columns: new[] { "tenant_id", "document_number_sequence_id" },
                principalTable: "document_number_sequences",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_issued_by_tenant_user_id_tenant_users",
                table: "receipts",
                column: "issued_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_outlet_id_outlets",
                table: "receipts",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_receipt_template_version_id_receipt_template_versions",
                table: "receipts",
                columns: new[] { "tenant_id", "receipt_template_version_id" },
                principalTable: "receipt_template_versions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_sales_order_id_sales_orders",
                table: "receipts",
                columns: new[] { "tenant_id", "sales_order_id" },
                principalTable: "sales_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_tenant_id_tenants",
                table: "receipts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_till_id_tills",
                table: "receipts",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_till_session_id_till_sessions",
                table: "receipts",
                columns: new[] { "tenant_id", "till_session_id" },
                principalTable: "till_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_cash_movements_performed_by_tenant_user_id_tenant_users",
                table: "till_cash_movements",
                column: "performed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_cash_movements_tenant_id_tenants",
                table: "till_cash_movements",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_cash_movements_till_session_id_till_sessions",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "till_session_id" },
                principalTable: "till_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_events_event_by_tenant_user_id_tenant_users",
                table: "till_session_events",
                column: "event_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_events_pos_device_id_pos_devices",
                table: "till_session_events",
                columns: new[] { "tenant_id", "pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_events_tenant_id_tenants",
                table: "till_session_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_events_till_session_id_till_sessions",
                table: "till_session_events",
                columns: new[] { "tenant_id", "till_session_id" },
                principalTable: "till_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_payment_summaries_payment_method_id_payment_methods",
                table: "till_session_payment_summaries",
                columns: new[] { "tenant_id", "payment_method_id" },
                principalTable: "payment_methods",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_payment_summaries_tenant_id_tenants",
                table: "till_session_payment_summaries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries",
                table: "till_session_payment_summaries",
                columns: new[] { "tenant_id", "till_session_summary_id" },
                principalTable: "till_session_summaries",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_approved_by_tenant_user_id_tenant_users",
                table: "till_session_summaries",
                column: "approved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_cashier_tenant_user_id_tenant_users",
                table: "till_session_summaries",
                column: "cashier_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_outlet_id_outlets",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_tenant_id_tenants",
                table: "till_session_summaries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_till_id_tills",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_till_session_id_till_sessions",
                table: "till_session_summaries",
                columns: new[] { "tenant_id", "till_session_id" },
                principalTable: "till_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_till_id_tills",
                table: "till_sessions",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pos_order_holds_held_by_tenant_user_id_tenant_users",
                table: "pos_order_holds");

            migrationBuilder.DropForeignKey(
                name: "fk_pos_order_holds_released_by_tenant_user_id_tenant_users",
                table: "pos_order_holds");

            migrationBuilder.DropForeignKey(
                name: "fk_pos_order_holds_sales_order_id_sales_orders",
                table: "pos_order_holds");

            migrationBuilder.DropForeignKey(
                name: "fk_pos_order_holds_tenant_id_tenants",
                table: "pos_order_holds");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_print_logs_operator_tenant_user_id_tenant_users",
                table: "receipt_print_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_print_logs_receipt_id_receipts",
                table: "receipt_print_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_print_logs_tenant_id_tenants",
                table: "receipt_print_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_outlet_id_outlets",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_pos_device_id_pos_devices",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_tenant_id_tenants",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_till_id_tills",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_versions_receipt_template_id_receipt_templates",
                table: "receipt_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_versions_tenant_id_tenants",
                table: "receipt_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_templates_parent_template_id_receipt_templates",
                table: "receipt_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_templates_tenant_id_tenants",
                table: "receipt_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_document_number_sequence_id_document_number_sequences",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_issued_by_tenant_user_id_tenant_users",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_outlet_id_outlets",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_receipt_template_version_id_receipt_template_versions",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_sales_order_id_sales_orders",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_tenant_id_tenants",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_till_id_tills",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_receipts_till_session_id_till_sessions",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_till_cash_movements_performed_by_tenant_user_id_tenant_users",
                table: "till_cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_till_cash_movements_tenant_id_tenants",
                table: "till_cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_till_cash_movements_till_session_id_till_sessions",
                table: "till_cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_events_event_by_tenant_user_id_tenant_users",
                table: "till_session_events");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_events_pos_device_id_pos_devices",
                table: "till_session_events");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_events_tenant_id_tenants",
                table: "till_session_events");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_events_till_session_id_till_sessions",
                table: "till_session_events");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_payment_summaries_payment_method_id_payment_methods",
                table: "till_session_payment_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_payment_summaries_tenant_id_tenants",
                table: "till_session_payment_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries",
                table: "till_session_payment_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_approved_by_tenant_user_id_tenant_users",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_cashier_tenant_user_id_tenant_users",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_outlet_id_outlets",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_tenant_id_tenants",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_till_id_tills",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_session_summaries_till_session_id_till_sessions",
                table: "till_session_summaries");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_till_id_tills",
                table: "till_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tills_tenant_id_id",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "uq_tills_tenant_id_id",
                table: "tills");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_till_sessions_tenant_id_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "uq_till_sessions_tenant_id_id",
                table: "till_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_till_session_summaries_tenant_id_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_summaries_approved_by_tenant_user_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_summaries_cashier_tenant_user_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_summaries_tenant_id_outlet_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_summaries_tenant_id_till_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "uq_till_session_summaries_tenant_id_id",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "uq_till_session_summaries_till_session_id",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_charge_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_counted_cash_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_discount_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_expected_cash_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_gross_sales_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_net_sales_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_opening_cash_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_refund_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_summary_status",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_tax_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_void_amount",
                table: "till_session_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_summaries_void_count",
                table: "till_session_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_payment_summaries_tenant_id_payment_method_id",
                table: "till_session_payment_summaries");

            migrationBuilder.DropIndex(
                name: "uq_till_session_payment_summaries_tenant_id_id",
                table: "till_session_payment_summaries");

            migrationBuilder.DropIndex(
                name: "uq_till_session_payment_summaries_till_session_summary_id_payment_method_id",
                table: "till_session_payment_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_payment_summaries_refund_amount",
                table: "till_session_payment_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_payment_summaries_sales_amount",
                table: "till_session_payment_summaries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_payment_summaries_transaction_count",
                table: "till_session_payment_summaries");

            migrationBuilder.DropIndex(
                name: "IX_till_session_events_event_by_tenant_user_id",
                table: "till_session_events");

            migrationBuilder.DropIndex(
                name: "IX_till_session_events_tenant_id_pos_device_id",
                table: "till_session_events");

            migrationBuilder.DropIndex(
                name: "IX_till_session_events_tenant_id_till_session_id",
                table: "till_session_events");

            migrationBuilder.DropIndex(
                name: "uq_till_session_events_tenant_id_id",
                table: "till_session_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_session_events_event_type",
                table: "till_session_events");

            migrationBuilder.DropIndex(
                name: "IX_till_cash_movements_performed_by_tenant_user_id",
                table: "till_cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_till_cash_movements_tenant_id_till_session_id",
                table: "till_cash_movements");

            migrationBuilder.DropIndex(
                name: "uq_till_cash_movements_tenant_id_id",
                table: "till_cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_cash_movements_movement_type",
                table: "till_cash_movements");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_sales_orders_tenant_id_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "uq_sales_orders_tenant_id_id",
                table: "sales_orders");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_receipts_tenant_id_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_issued_by_tenant_user_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_tenant_id_document_number_sequence_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_tenant_id_outlet_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_tenant_id_receipt_template_version_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_tenant_id_sales_order_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_tenant_id_till_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_tenant_id_till_session_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "uq_receipts_tenant_id_id",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_change_amount",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_charge_amount",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_discount_amount",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_paid_amount",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_receipt_status",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_receipt_type",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_subtotal_amount",
                table: "receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipts_tax_amount",
                table: "receipts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_receipt_templates_tenant_id_id",
                table: "receipt_templates");

            migrationBuilder.DropIndex(
                name: "IX_receipt_templates_tenant_id_parent_template_id",
                table: "receipt_templates");

            migrationBuilder.DropIndex(
                name: "uq_receipt_templates_tenant_id_id",
                table: "receipt_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_templates_status",
                table: "receipt_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_templates_template_type",
                table: "receipt_templates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_receipt_template_versions_tenant_id_id",
                table: "receipt_template_versions");

            migrationBuilder.DropIndex(
                name: "uq_receipt_template_versions_receipt_template_id_version_number",
                table: "receipt_template_versions");

            migrationBuilder.DropIndex(
                name: "uq_receipt_template_versions_tenant_id_id",
                table: "receipt_template_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_template_versions_effective_dates",
                table: "receipt_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_tenant_id_outlet_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_tenant_id_pos_device_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_tenant_id_receipt_template_ver~",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_tenant_id_till_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "uq_receipt_template_assignments_tenant_id_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_template_assignments_assignment_scope",
                table: "receipt_template_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_template_assignments_effective_dates",
                table: "receipt_template_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_template_assignments_scope_rules",
                table: "receipt_template_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_template_assignments_status",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "IX_receipt_print_logs_operator_tenant_user_id",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "uq_receipt_print_logs_receipt_id_attempt_number",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "uq_receipt_print_logs_tenant_id_id",
                table: "receipt_print_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_print_logs_print_status",
                table: "receipt_print_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_receipt_print_logs_printed_copy_type",
                table: "receipt_print_logs");

            migrationBuilder.DropIndex(
                name: "IX_pos_order_holds_held_by_tenant_user_id",
                table: "pos_order_holds");

            migrationBuilder.DropIndex(
                name: "IX_pos_order_holds_released_by_tenant_user_id",
                table: "pos_order_holds");

            migrationBuilder.DropIndex(
                name: "IX_pos_order_holds_tenant_id_sales_order_id",
                table: "pos_order_holds");

            migrationBuilder.DropIndex(
                name: "uq_pos_order_holds_tenant_id_id",
                table: "pos_order_holds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pos_order_holds_hold_status",
                table: "pos_order_holds");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_pos_devices_tenant_id_id",
                table: "pos_devices");

            migrationBuilder.DropIndex(
                name: "uq_pos_devices_tenant_id_id",
                table: "pos_devices");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_payment_methods_tenant_id_id",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "uq_payment_methods_tenant_id_id",
                table: "payment_methods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_document_number_sequences_tenant_id_id",
                table: "document_number_sequences");

            migrationBuilder.DropIndex(
                name: "uq_document_number_sequences_tenant_id_id",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "approved_by_tenant_user_id",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "cash_difference_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "cashier_tenant_user_id",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "charge_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "counted_cash_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "expected_cash_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "generated_at",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "gross_sales_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "net_sales_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "opening_cash_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "session_closed_at",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "session_opened_at",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "summary_status",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "void_amount",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "void_count",
                table: "till_session_summaries");

            migrationBuilder.DropColumn(
                name: "net_amount",
                table: "till_session_payment_summaries");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "till_session_payment_summaries");

            migrationBuilder.DropColumn(
                name: "sales_amount",
                table: "till_session_payment_summaries");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "till_session_payment_summaries");

            migrationBuilder.DropColumn(
                name: "transaction_count",
                table: "till_session_payment_summaries");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "event_at",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "event_by_tenant_user_id",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "event_payload_json",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "pos_device_id",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "reference_id",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "reference_type",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "movement_type",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "performed_at",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "performed_by_tenant_user_id",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "reference_number",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "business_date",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "change_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "charge_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "issued_at",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "issued_by_tenant_user_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "receipt_data_json",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "receipt_status",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "receipt_template_version_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "receipt_type",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "rounding_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "till_session_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "is_base_template",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "template_type",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "effective_to",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "page_size",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "template_data",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "effective_to",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "operator_tenant_user_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "print_result_json",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "print_status",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printed_at",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printed_copy_type",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "printer_device_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "receipt_print_logs");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "held_at",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "held_by_tenant_user_id",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "hold_reason",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "hold_status",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "released_at",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "released_by_tenant_user_id",
                table: "pos_order_holds");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "till_session_events",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "till_cash_movements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "receipts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "document_number_sequence_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "receipt_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "till_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "pos_device_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "pos_order_holds",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_till_id",
                table: "till_sessions",
                column: "till_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_session_summaries_till_session_id",
                table: "till_session_summaries",
                column: "till_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_till_session_payment_summaries_payment_method_id",
                table: "till_session_payment_summaries",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "uq_till_session_payment_summaries_till_session_summary_id_payment_method_id",
                table: "till_session_payment_summaries",
                columns: new[] { "till_session_summary_id", "payment_method_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_till_session_events_till_session_id",
                table: "till_session_events",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_cash_movements_till_session_id",
                table: "till_cash_movements",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_document_number_sequence_id",
                table: "receipts",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_sales_order_id",
                table: "receipts",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_templates_parent_template_id",
                table: "receipt_templates",
                column: "parent_template_id");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_template_versions_receipt_template_id_version_number",
                table: "receipt_template_versions",
                columns: new[] { "receipt_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_receipt_template_version_id",
                table: "receipt_template_assignments",
                column: "receipt_template_version_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receipt_template_assignments_assignment_scope_outlet_id_til~",
                table: "receipt_template_assignments",
                sql: "(assignment_scope = 'OUTLET' AND outlet_id IS NOT NULL) OR (assignment_scope = 'TILL' AND till_id IS NOT NULL) OR (assignment_scope = 'POS_DEVICE' AND pos_device_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "uq_receipt_print_logs_receipt_id_attempt_number",
                table: "receipt_print_logs",
                columns: new[] { "receipt_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_order_holds_sales_order_id",
                table: "pos_order_holds",
                column: "sales_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_order_holds_sales_order_id_sales_orders",
                table: "pos_order_holds",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_print_logs_receipt_id_receipts",
                table: "receipt_print_logs",
                column: "receipt_id",
                principalTable: "receipts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_receipt_template_version_id_receipt_template_versions",
                table: "receipt_template_assignments",
                column: "receipt_template_version_id",
                principalTable: "receipt_template_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_versions_receipt_template_id_receipt_templates",
                table: "receipt_template_versions",
                column: "receipt_template_id",
                principalTable: "receipt_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_templates_parent_template_id_receipt_templates",
                table: "receipt_templates",
                column: "parent_template_id",
                principalTable: "receipt_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_document_number_sequence_id_document_number_sequences",
                table: "receipts",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_sales_order_id_sales_orders",
                table: "receipts",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_cash_movements_till_session_id_till_sessions",
                table: "till_cash_movements",
                column: "till_session_id",
                principalTable: "till_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_events_till_session_id_till_sessions",
                table: "till_session_events",
                column: "till_session_id",
                principalTable: "till_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_payment_summaries_payment_method_id_payment_methods",
                table: "till_session_payment_summaries",
                column: "payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_payment_summaries_till_session_summary_id_till_session_summaries",
                table: "till_session_payment_summaries",
                column: "till_session_summary_id",
                principalTable: "till_session_summaries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_session_summaries_till_session_id_till_sessions",
                table: "till_session_summaries",
                column: "till_session_id",
                principalTable: "till_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_till_id_tills",
                table: "till_sessions",
                column: "till_id",
                principalTable: "tills",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
