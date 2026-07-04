using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModules24To28WithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_platform_integration_id_platform_integrations",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_delivery_attempts_notification_channel_id_notification_channels",
                table: "notification_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_replacement_order_id_sales_orders",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_return_amount",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_requested_quantity",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "uq_sales_return_events_sales_return_id_sequence_number",
                table: "sales_return_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_events_sequence_number",
                table: "sales_return_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_refunded_amount",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_requested_amount",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_amount",
                table: "sales_refund_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_requested_amount",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "uq_sales_payment_transactions_idempotency_key",
                table: "sales_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_transactions_amount",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "uq_sales_payment_events_sales_payment_id_sequence_number",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_replacement_order_id",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchanges_additional_amount",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchanges_refund_amount",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchange_lines_replacement_quantity",
                table: "sales_exchange_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchange_lines_returned_quantity",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "uq_sales_exchange_events_sales_exchange_id_sequence_number",
                table: "sales_exchange_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchange_events_sequence_number",
                table: "sales_exchange_events");

            migrationBuilder.DropIndex(
                name: "uq_return_inspections_tenant_id_inspection_number",
                table: "return_inspections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_inspections_inspected_quantity",
                table: "return_inspections");

            migrationBuilder.DropIndex(
                name: "uq_notification_templates_tenant_id_notification_event_type_id_channel_type_locale_template_code",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "uq_notification_template_versions_notification_template_id",
                table: "notification_template_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_read_receipts_read_source",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "uq_notification_events_tenant_id_idempotency_key",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_attempts_notification_channel_id",
                table: "notification_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_platform_integration_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "total_return_amount",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "sequence_number",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "additional_amount",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "replacement_order_id",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "replacement_quantity",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "sequence_number",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "description",
                table: "return_reasons");

            migrationBuilder.DropColumn(
                name: "name",
                table: "return_reasons");

            migrationBuilder.DropColumn(
                name: "inspected_quantity",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "inspection_number",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "name",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "name",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "description",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "name",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "name",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "name",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "name",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "name",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "platform_integration_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "description",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "name",
                table: "integration_providers");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "sync_items",
                newName: "item_status");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "sync_batches",
                newName: "sync_status");

            migrationBuilder.RenameColumn(
                name: "requested_quantity",
                table: "sales_return_lines",
                newName: "unit_price_snapshot");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "sales_exchanges",
                newName: "exchange_status");

            migrationBuilder.RenameColumn(
                name: "returned_quantity",
                table: "sales_exchange_lines",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "platform_integrations",
                newName: "integration_status");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "platform_integrations",
                newName: "last_failure_reason");

            migrationBuilder.RenameColumn(
                name: "read_source",
                table: "notification_read_receipts",
                newName: "recipient_type");

            migrationBuilder.RenameColumn(
                name: "idempotency_key",
                table: "notification_events",
                newName: "event_code");

            migrationBuilder.RenameColumn(
                name: "notification_channel_id",
                table: "notification_delivery_attempts",
                newName: "tenant_id");

            migrationBuilder.AlterColumn<string>(
                name: "payload_hash",
                table: "sync_items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "operation_type",
                table: "sync_items",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "entity_name",
                table: "sync_items",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "client_record_id",
                table: "sync_items",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "direction",
                table: "sync_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "sync_items",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "sync_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "sync_items",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processed_at",
                table: "sync_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "received_at",
                table: "sync_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "server_record_id",
                table: "sync_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sync_item_id",
                table: "sync_conflicts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "client_payload_json",
                table: "sync_conflicts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_record_id",
                table: "sync_conflicts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "conflict_type",
                table: "sync_conflicts",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "entity_name",
                table: "sync_conflicts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "resolution_note",
                table: "sync_conflicts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_strategy",
                table: "sync_conflicts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at",
                table: "sync_conflicts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_tenant_user_id",
                table: "sync_conflicts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "server_payload_json",
                table: "sync_conflicts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "server_record_id",
                table: "sync_conflicts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sync_conflicts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "uploaded_item_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "sync_batches",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<int>(
                name: "downloaded_item_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "conflict_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "client_app_version",
                table: "sync_batches",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "client_local_time",
                table: "sync_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "client_started_at",
                table: "sync_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "sync_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "sync_batches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "sync_batches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "server_started_at",
                table: "sync_batches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "sync_type",
                table: "sync_batches",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_refund_amount",
                table: "sales_returns",
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
                table: "sales_returns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "sales_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "sales_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "sales_returns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "sales_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "received_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "return_channel",
                table: "sales_returns",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "return_reason_id",
                table: "sales_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_exchange_amount",
                table: "sales_returns",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_received_qty",
                table: "sales_returns",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_requested_qty",
                table: "sales_returns",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "sales_returns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "disposition_status",
                table: "sales_return_lines",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_subtotal_amount",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_tax_amount",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "sales_return_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_received",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_requested",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "return_reason_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_tax_amount_snapshot",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_id",
                table: "sales_return_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_return_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_notes",
                table: "sales_return_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "sales_return_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "sales_return_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "sales_return_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_return_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_refunds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "requested_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "approved_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "sales_refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_tenant_user_id",
                table: "sales_refunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "sales_refunds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "sales_refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "sales_refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "sales_refunds",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "sales_refunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_mode",
                table: "sales_refunds",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                table: "sales_refunds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "sales_refunds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<decimal>(
                name: "allocated_amount",
                table: "sales_refund_payment_allocations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "allocation_status",
                table: "sales_refund_payment_allocations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "external_reference",
                table: "sales_refund_payment_allocations",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "refund_payment_method_id",
                table: "sales_refund_payment_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "refund_transaction_id",
                table: "sales_refund_payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_refund_payment_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_line_id",
                table: "sales_refund_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "sales_refund_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "description_snapshot",
                table: "sales_refund_lines",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "sales_refund_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_line_type",
                table: "sales_refund_lines",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_refund_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "requested_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "paid_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "sales_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "sales_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "change_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "sales_payments",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "sales_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_reference",
                table: "sales_payments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "sales_payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "initiated_at",
                table: "sales_payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paid_at",
                table: "sales_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_note",
                table: "sales_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tendered_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "sales_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_session_id",
                table: "sales_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "sales_payment_transactions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "sales_payment_transactions",
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
                table: "sales_payment_transactions",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "external_transaction_reference",
                table: "sales_payment_transactions",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_transaction_id",
                table: "sales_payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processed_at",
                table: "sales_payment_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "processed_by_tenant_user_id",
                table: "sales_payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "sales_payment_transactions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_response_json",
                table: "sales_payment_transactions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_payment_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "transaction_status",
                table: "sales_payment_transactions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "transaction_type",
                table: "sales_payment_transactions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "event_at",
                table: "sales_payment_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "event_by_tenant_user_id",
                table: "sales_payment_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_note",
                table: "sales_payment_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_payload_json",
                table: "sales_payment_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "sales_payment_events",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "sales_payment_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "sales_payment_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_payment_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "additional_payment_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "sales_exchanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "sales_exchanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_exchanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "sales_exchanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_mode",
                table: "sales_exchanges",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "sales_exchanges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_difference_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_back_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_sales_order_id",
                table: "sales_exchanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "sales_exchanges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_line_id",
                table: "sales_exchange_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "exchange_action_type",
                table: "sales_exchange_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "net_difference_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "original_line_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "replacement_line_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_product_id",
                table: "sales_exchange_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_product_variant_id",
                table: "sales_exchange_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_sales_order_line_id",
                table: "sales_exchange_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_exchange_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_exchange_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_notes",
                table: "sales_exchange_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "sales_exchange_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "sales_exchange_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "sales_exchange_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_exchange_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "applies_to",
                table: "return_reasons",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "return_reasons",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "reason_name",
                table: "return_reasons",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_inspection",
                table: "return_reasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "return_reasons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "condition_code",
                table: "return_inspections",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "inspected_at",
                table: "return_inspections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inspected_by_tenant_user_id",
                table: "return_inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inspection_notes",
                table: "return_inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inspection_status",
                table: "return_inspections",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_location_id",
                table: "return_inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "reject_quantity",
                table: "return_inspections",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "restock_decision",
                table: "return_inspections",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "restock_quantity",
                table: "return_inspections",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "currency_code",
                table: "platform_integrations",
                type: "char(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<string>(
                name: "base_url",
                table: "platform_integrations",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "connected_at",
                table: "platform_integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "platform_integrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "disconnected_at",
                table: "platform_integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "environment",
                table: "platform_integrations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "inbound_webhook_url",
                table: "platform_integrations",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "integration_category",
                table: "platform_integrations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "integration_name",
                table: "platform_integrations",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "platform_integrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "platform_integrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_failed_request_at",
                table: "platform_integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_successful_request_at",
                table: "platform_integrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_integration_id",
                table: "platform_integration_webhook_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "platform_integration_webhook_events",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "external_event_id",
                table: "platform_integration_webhook_events",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "event_category",
                table: "platform_integration_webhook_events",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_name",
                table: "platform_integration_webhook_events",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "event_payload_json",
                table: "platform_integration_webhook_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_status",
                table: "platform_integration_webhook_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "headers_json",
                table: "platform_integration_webhook_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processed_at",
                table: "platform_integration_webhook_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_error",
                table: "platform_integration_webhook_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_started_at",
                table: "platform_integration_webhook_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "received_at",
                table: "platform_integration_webhook_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "received_signature_hash",
                table: "platform_integration_webhook_events",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "signature_valid",
                table: "platform_integration_webhook_events",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_ip",
                table: "platform_integration_webhook_events",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "platform_integration_webhook_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_integration_id",
                table: "platform_integration_request_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "duration_ms",
                table: "platform_integration_request_logs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "platform_integration_request_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "platform_integration_request_logs",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "platform_integration_request_logs",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "platform_integration_request_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "http_method",
                table: "platform_integration_request_logs",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "platform_integration_request_logs",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_body_hash",
                table: "platform_integration_request_logs",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_direction",
                table: "platform_integration_request_logs",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_headers_json",
                table: "platform_integration_request_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_status",
                table: "platform_integration_request_logs",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_type",
                table: "platform_integration_request_logs",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "request_url",
                table: "platform_integration_request_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "platform_integration_request_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "response_body_hash",
                table: "platform_integration_request_logs",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "response_headers_json",
                table: "platform_integration_request_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "platform_integration_request_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_integration_credentials",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "credential_name",
                table: "platform_integration_credentials",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "platform_integration_credentials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credential_key_version",
                table: "platform_integration_credentials",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credential_type",
                table: "platform_integration_credentials",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "encrypted_value",
                table: "platform_integration_credentials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "encryption_key_id",
                table: "platform_integration_credentials",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "platform_integration_credentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_rotated_at",
                table: "platform_integration_credentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "platform_integration_credentials",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "method_code",
                table: "payment_methods",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<bool>(
                name: "allows_change",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_active_for_online",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_active_for_pos",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "method_name",
                table: "payment_methods",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_manual_confirmation",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_reference",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "payment_methods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "supports_refund",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<long>(
                name: "range_start",
                table: "offline_number_blocks",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "range_end",
                table: "offline_number_blocks",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "padding_length_snapshot",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "next_value",
                table: "offline_number_blocks",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "document_number_sequence_id",
                table: "offline_number_blocks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "allocated_at",
                table: "offline_number_blocks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "block_status",
                table: "offline_number_blocks",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_type",
                table: "offline_number_blocks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exhausted_at",
                table: "offline_number_blocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "offline_number_blocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prefix_snapshot",
                table: "offline_number_blocks",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suffix_snapshot",
                table: "offline_number_blocks",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "offline_number_blocks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "entity_name",
                table: "offline_id_mappings",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_from_sync_item_id",
                table: "offline_id_mappings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "client_record_id",
                table: "offline_id_mappings",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "pos_device_id",
                table: "offline_clients",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "offline_clients",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "max_offline_duration_minutes",
                table: "offline_clients",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "client_key_hash",
                table: "offline_clients",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_name",
                table: "offline_clients",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "offline_clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_seen_at",
                table: "offline_clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_sync_at",
                table: "offline_clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "offline_enabled",
                table: "offline_clients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "offline_type",
                table: "offline_clients",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "offline_clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "template_code",
                table: "notification_templates",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                table: "notification_templates",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "notification_templates",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "template_scope",
                table: "notification_templates",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "action_label_template",
                table: "notification_template_versions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "action_url_template",
                table: "notification_template_versions",
                type: "varchar(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_html_template",
                table: "notification_template_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_text_template",
                table: "notification_template_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sample_payload_json",
                table: "notification_template_versions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "notification_template_versions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "subject_template",
                table: "notification_template_versions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "notification_template_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_template",
                table: "notification_template_versions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "variables_schema_json",
                table: "notification_template_versions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "notification_read_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "notification_read_receipts",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "notification_message_id",
                table: "notification_read_receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "platform_user_id",
                table: "notification_read_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "read_at",
                table: "notification_read_receipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "notification_read_receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_user_id",
                table: "notification_read_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "notification_read_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "notification_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "notification_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "notification_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "notification_template_version_id",
                table: "notification_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "notification_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "action_url_mapped",
                table: "notification_messages",
                type: "varchar(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_html_mapped",
                table: "notification_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_text_mapped",
                table: "notification_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channel_type",
                table: "notification_messages",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at",
                table: "notification_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "notification_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "notification_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "message_status",
                table: "notification_messages",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "message_type",
                table: "notification_messages",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "notification_messages",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "recipient_address_json",
                table: "notification_messages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_email",
                table: "notification_messages",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_name",
                table: "notification_messages",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_phone",
                table: "notification_messages",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_at",
                table: "notification_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sent_at",
                table: "notification_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_text",
                table: "notification_messages",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "notification_inbox_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_text",
                table: "notification_inbox_items",
                type: "varchar(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "notification_inbox_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at",
                table: "notification_inbox_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "notification_inbox_items",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "link_url",
                table: "notification_inbox_items",
                type: "varchar(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "platform_user_id",
                table: "notification_inbox_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "read_at",
                table: "notification_inbox_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_type",
                table: "notification_inbox_items",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "notification_inbox_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_user_id",
                table: "notification_inbox_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_text",
                table: "notification_inbox_items",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "notification_inbox_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "notification_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "notification_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "notification_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "notification_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_status",
                table: "notification_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "notification_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "notification_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "notification_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processed_at",
                table: "notification_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_started_at",
                table: "notification_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_at",
                table: "notification_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_module",
                table: "notification_events",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_reference_id",
                table: "notification_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_reference_type",
                table: "notification_events",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "notification_event_types",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "event_code",
                table: "notification_event_types",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "default_priority",
                table: "notification_event_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "event_name",
                table: "notification_event_types",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "notification_event_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system_event",
                table: "notification_event_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "source_module",
                table: "notification_event_types",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "notification_event_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "attempted_at",
                table: "notification_delivery_attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "channel_type",
                table: "notification_delivery_attempts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "notification_delivery_attempts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "notification_delivery_attempts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "notification_delivery_attempts",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "notification_delivery_attempts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_payload_json",
                table: "notification_delivery_attempts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "response_payload_json",
                table: "notification_delivery_attempts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "response_status_code",
                table: "notification_delivery_attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "channel_code",
                table: "notification_channels",
                type: "varchar(90)",
                maxLength: 90,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "channel_name",
                table: "notification_channels",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "notification_channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system_channel",
                table: "notification_channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "provider_config_json",
                table: "notification_channels",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "notification_channels",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "api_base_url",
                table: "integration_providers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "auth_type",
                table: "integration_providers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "documentation_url",
                table: "integration_providers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "integration_providers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_type",
                table: "integration_providers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "integration_providers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "supports_test_mode",
                table: "integration_providers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "supports_webhook",
                table: "integration_providers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "last_server_version",
                table: "device_sync_states",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "last_client_version",
                table: "device_sync_states",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_full_sync_at",
                table: "device_sync_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_incremental_sync_at",
                table: "device_sync_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "device_sync_states",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sync_direction",
                table: "device_sync_states",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sync_filter_json",
                table: "device_sync_states",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_resolved_by_tenant_user_id",
                table: "sync_conflicts",
                column: "resolved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_return_reason_id",
                table: "sales_returns",
                column: "return_reason_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_exchange_amount",
                table: "sales_returns",
                sql: "total_exchange_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_received_qty",
                table: "sales_returns",
                sql: "total_received_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_requested_qty",
                table: "sales_returns",
                sql: "total_requested_qty >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_return_reason_id",
                table: "sales_return_lines",
                column: "return_reason_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_line_subtotal_amount",
                table: "sales_return_lines",
                sql: "line_subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_line_tax_amount",
                table: "sales_return_lines",
                sql: "line_tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_quantity_received",
                table: "sales_return_lines",
                sql: "quantity_received IS NULL OR quantity_received >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_quantity_requested",
                table: "sales_return_lines",
                sql: "quantity_requested > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_unit_price_snapshot",
                table: "sales_return_lines",
                sql: "unit_price_snapshot >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_unit_tax_amount_snapshot",
                table: "sales_return_lines",
                sql: "unit_tax_amount_snapshot >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_events_sales_return_id",
                table: "sales_return_events",
                column: "sales_return_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_approved_amount",
                table: "sales_refunds",
                sql: "approved_amount <= requested_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_refunded_amount",
                table: "sales_refunds",
                sql: "refunded_amount <= approved_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_requested_amount",
                table: "sales_refunds",
                sql: "requested_amount > 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_refund_payment_method_id",
                table: "sales_refund_payment_allocations",
                column: "refund_payment_method_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_payment_allocations_allocation_status",
                table: "sales_refund_payment_allocations",
                sql: "allocation_status IN ('PENDING', 'SUCCESS', 'FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_amount",
                table: "sales_refund_lines",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_quantity",
                table: "sales_refund_lines",
                sql: "quantity IS NULL OR quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payments_tenant_id_idempotency_key",
                table: "sales_payments",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_change_amount",
                table: "sales_payments",
                sql: "change_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_refunded_amount",
                table: "sales_payments",
                sql: "refunded_amount <= paid_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_requested_amount",
                table: "sales_payments",
                sql: "requested_amount IS NULL OR requested_amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_tendered_amount",
                table: "sales_payments",
                sql: "tendered_amount IS NULL OR tendered_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_transactions_parent_transaction_id",
                table: "sales_payment_transactions",
                column: "parent_transaction_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_transactions_provider_ext_ref",
                table: "sales_payment_transactions",
                columns: new[] { "tenant_id", "provider_name", "external_transaction_reference" },
                unique: true,
                filter: "provider_name IS NOT NULL AND external_transaction_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_transactions_tenant_id_idempotency_key",
                table: "sales_payment_transactions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_transactions_amount",
                table: "sales_payment_transactions",
                sql: "amount > 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_events_sales_payment_id",
                table: "sales_payment_events",
                column: "sales_payment_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_events_sales_payment_id_sequence_number",
                table: "sales_payment_events",
                columns: new[] { "tenant_id", "sales_payment_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_replacement_sales_order_id",
                table: "sales_exchanges",
                column: "replacement_sales_order_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchanges_additional_payment_amount",
                table: "sales_exchanges",
                sql: "additional_payment_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchanges_price_difference_amount",
                table: "sales_exchanges",
                sql: "price_difference_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchanges_refund_back_amount",
                table: "sales_exchanges",
                sql: "refund_back_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchange_lines_quantity",
                table: "sales_exchange_lines",
                sql: "quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_events_sales_exchange_id",
                table: "sales_exchange_events",
                column: "sales_exchange_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_reasons_sort_order",
                table: "return_reasons",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_inspections_reject_quantity",
                table: "return_inspections",
                sql: "reject_quantity IS NULL OR reject_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_inspections_restock_quantity",
                table: "return_inspections",
                sql: "restock_quantity IS NULL OR restock_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integrations_created_by_platform_user_id",
                table: "platform_integrations",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_credentials_created_by_platform_user_id",
                table: "platform_integration_credentials",
                column: "created_by_platform_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_sort_order",
                table: "payment_methods",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_tenant_id_template_code",
                table: "notification_templates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_notification_message_id",
                table: "notification_read_receipts",
                column: "notification_message_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_read_receipts_recipient_type_platform_user_id_~",
                table: "notification_read_receipts",
                sql: "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_customer_id",
                table: "notification_preferences",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_platform_user_id",
                table: "notification_preferences",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_tenant_user_id",
                table: "notification_preferences",
                column: "tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_inbox_items_recipient_type_platform_user_id_te~",
                table: "notification_inbox_items",
                sql: "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_events_priority",
                table: "notification_events",
                sql: "priority IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')");

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_customer_id_customers",
                table: "notification_preferences",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_platform_user_id_platform_users",
                table: "notification_preferences",
                column: "platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_tenant_user_id_tenant_users",
                table: "notification_preferences",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_notification_message_id_notification_messages",
                table: "notification_read_receipts",
                column: "notification_message_id",
                principalTable: "notification_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_credentials_created_by_platform_user_id_platform_users",
                table: "platform_integration_credentials",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integrations_created_by_platform_user_id_platform_users",
                table: "platform_integrations",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_replacement_sales_order_id_sales_orders",
                table: "sales_exchanges",
                column: "replacement_sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_transactions_parent_transaction_id_sales_payment_transactions",
                table: "sales_payment_transactions",
                column: "parent_transaction_id",
                principalTable: "sales_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_refund_payment_method_id_payment_methods",
                table: "sales_refund_payment_allocations",
                column: "refund_payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_return_reason_id_return_reasons",
                table: "sales_return_lines",
                column: "return_reason_id",
                principalTable: "return_reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_return_reason_id_return_reasons",
                table: "sales_returns",
                column: "return_reason_id",
                principalTable: "return_reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_resolved_by_tenant_user_id_tenant_users",
                table: "sync_conflicts",
                column: "resolved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_customer_id_customers",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_platform_user_id_platform_users",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_tenant_user_id_tenant_users",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_notification_message_id_notification_messages",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_credentials_created_by_platform_user_id_platform_users",
                table: "platform_integration_credentials");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integrations_created_by_platform_user_id_platform_users",
                table: "platform_integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_replacement_sales_order_id_sales_orders",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_parent_transaction_id_sales_payment_transactions",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_refund_payment_method_id_payment_methods",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_return_reason_id_return_reasons",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_return_reason_id_return_reasons",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_resolved_by_tenant_user_id_tenant_users",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sync_conflicts_resolved_by_tenant_user_id",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_return_reason_id",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_exchange_amount",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_received_qty",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_requested_qty",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_lines_return_reason_id",
                table: "sales_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_line_subtotal_amount",
                table: "sales_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_line_tax_amount",
                table: "sales_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_quantity_received",
                table: "sales_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_quantity_requested",
                table: "sales_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_unit_price_snapshot",
                table: "sales_return_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_return_lines_unit_tax_amount_snapshot",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_events_sales_return_id",
                table: "sales_return_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_approved_amount",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_refunded_amount",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_requested_amount",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_refund_payment_method_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_payment_allocations_allocation_status",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_amount",
                table: "sales_refund_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_quantity",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "uq_sales_payments_tenant_id_idempotency_key",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_change_amount",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_refunded_amount",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_requested_amount",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_tendered_amount",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_transactions_parent_transaction_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "uq_sales_payment_transactions_provider_ext_ref",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "uq_sales_payment_transactions_tenant_id_idempotency_key",
                table: "sales_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_transactions_amount",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_events_sales_payment_id",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "uq_sales_payment_events_sales_payment_id_sequence_number",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_replacement_sales_order_id",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchanges_additional_payment_amount",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchanges_price_difference_amount",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchanges_refund_back_amount",
                table: "sales_exchanges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchange_lines_quantity",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_events_sales_exchange_id",
                table: "sales_exchange_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_reasons_sort_order",
                table: "return_reasons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_inspections_reject_quantity",
                table: "return_inspections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_inspections_restock_quantity",
                table: "return_inspections");

            migrationBuilder.DropIndex(
                name: "IX_platform_integrations_created_by_platform_user_id",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_credentials_created_by_platform_user_id",
                table: "platform_integration_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_sort_order",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "uq_notification_templates_tenant_id_template_code",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_notification_message_id",
                table: "notification_read_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_read_receipts_recipient_type_platform_user_id_~",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "IX_notification_preferences_customer_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_preferences_platform_user_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_preferences_tenant_user_id",
                table: "notification_preferences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_inbox_items_recipient_type_platform_user_id_te~",
                table: "notification_inbox_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_events_priority",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "direction",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "server_record_id",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "client_payload_json",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "client_record_id",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "conflict_type",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "entity_name",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "resolution_note",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "resolution_strategy",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "resolved_at",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "resolved_by_tenant_user_id",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "server_payload_json",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "server_record_id",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sync_conflicts");

            migrationBuilder.DropColumn(
                name: "client_app_version",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "client_local_time",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "client_started_at",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "server_started_at",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "sync_type",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "return_channel",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "return_reason_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "total_exchange_amount",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "total_received_qty",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "total_requested_qty",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "disposition_status",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "line_subtotal_amount",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "line_tax_amount",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "quantity_received",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "quantity_requested",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "return_reason_id",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "unit_tax_amount_snapshot",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "event_notes",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "approved_amount",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "approved_by_tenant_user_id",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "refund_mode",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "refund_reason",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "allocation_status",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropColumn(
                name: "external_reference",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropColumn(
                name: "refund_payment_method_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropColumn(
                name: "refund_transaction_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropColumn(
                name: "description_snapshot",
                table: "sales_refund_lines");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "sales_refund_lines");

            migrationBuilder.DropColumn(
                name: "refund_line_type",
                table: "sales_refund_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_refund_lines");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "change_amount",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "external_reference",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "initiated_at",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "payment_note",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "tendered_amount",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "till_session_id",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "external_transaction_reference",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "parent_transaction_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "processed_by_tenant_user_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_response_json",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "transaction_status",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "transaction_type",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "event_at",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "event_by_tenant_user_id",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "event_note",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "event_payload_json",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "additional_payment_amount",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "exchange_mode",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "price_difference_amount",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "refund_back_amount",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "replacement_sales_order_id",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "sales_exchanges");

            migrationBuilder.DropColumn(
                name: "exchange_action_type",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "net_difference_amount",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "original_line_amount",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "replacement_line_amount",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "replacement_product_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "replacement_product_variant_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "replacement_sales_order_line_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "event_notes",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "return_reasons");

            migrationBuilder.DropColumn(
                name: "reason_name",
                table: "return_reasons");

            migrationBuilder.DropColumn(
                name: "requires_inspection",
                table: "return_reasons");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "return_reasons");

            migrationBuilder.DropColumn(
                name: "condition_code",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "inspected_at",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "inspected_by_tenant_user_id",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "inspection_notes",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "inspection_status",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "inventory_location_id",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "reject_quantity",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "restock_decision",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "restock_quantity",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "base_url",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "connected_at",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "disconnected_at",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "environment",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "inbound_webhook_url",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "integration_category",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "integration_name",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "last_failed_request_at",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "last_successful_request_at",
                table: "platform_integrations");

            migrationBuilder.DropColumn(
                name: "event_category",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "event_name",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "event_payload_json",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "event_status",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "headers_json",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "processing_error",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "processing_started_at",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "received_signature_hash",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "signature_valid",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "source_ip",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "http_method",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "request_body_hash",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "request_direction",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "request_headers_json",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "request_status",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "request_type",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "request_url",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "response_body_hash",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "response_headers_json",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "credential_key_version",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "credential_type",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "encrypted_value",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "encryption_key_id",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "last_rotated_at",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "status",
                table: "platform_integration_credentials");

            migrationBuilder.DropColumn(
                name: "allows_change",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "is_active_for_online",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "is_active_for_pos",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "method_name",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "requires_manual_confirmation",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "requires_reference",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "supports_refund",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "allocated_at",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "block_status",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "document_type",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "exhausted_at",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "prefix_snapshot",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "suffix_snapshot",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "offline_number_blocks");

            migrationBuilder.DropColumn(
                name: "client_key_hash",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "client_name",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "last_sync_at",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "offline_enabled",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "offline_type",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "offline_clients");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "template_scope",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "action_label_template",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "action_url_template",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "body_html_template",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "body_text_template",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "sample_payload_json",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "subject_template",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "title_template",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "variables_schema_json",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "notification_message_id",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "platform_user_id",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "read_at",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "tenant_user_id",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "action_url_mapped",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "body_html_mapped",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "body_text_mapped",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "channel_type",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "message_status",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "message_type",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "recipient_address_json",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "recipient_email",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "recipient_name",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "recipient_phone",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "title_text",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "body_text",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "link_url",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "platform_user_id",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "read_at",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "recipient_type",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "tenant_user_id",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "title_text",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "event_status",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "processing_started_at",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "source_module",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "source_reference_id",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "source_reference_type",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "event_name",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "is_system_event",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "source_module",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "status",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "attempted_at",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "channel_type",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "request_payload_json",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "response_payload_json",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "response_status_code",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "channel_name",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "is_system_channel",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "provider_config_json",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "api_base_url",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "auth_type",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "documentation_url",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "provider_type",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "status",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "supports_test_mode",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "supports_webhook",
                table: "integration_providers");

            migrationBuilder.DropColumn(
                name: "last_full_sync_at",
                table: "device_sync_states");

            migrationBuilder.DropColumn(
                name: "last_incremental_sync_at",
                table: "device_sync_states");

            migrationBuilder.DropColumn(
                name: "status",
                table: "device_sync_states");

            migrationBuilder.DropColumn(
                name: "sync_direction",
                table: "device_sync_states");

            migrationBuilder.DropColumn(
                name: "sync_filter_json",
                table: "device_sync_states");

            migrationBuilder.RenameColumn(
                name: "item_status",
                table: "sync_items",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "sync_status",
                table: "sync_batches",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "unit_price_snapshot",
                table: "sales_return_lines",
                newName: "requested_quantity");

            migrationBuilder.RenameColumn(
                name: "exchange_status",
                table: "sales_exchanges",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "sales_exchange_lines",
                newName: "returned_quantity");

            migrationBuilder.RenameColumn(
                name: "last_failure_reason",
                table: "platform_integrations",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "integration_status",
                table: "platform_integrations",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "recipient_type",
                table: "notification_read_receipts",
                newName: "read_source");

            migrationBuilder.RenameColumn(
                name: "event_code",
                table: "notification_events",
                newName: "idempotency_key");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "notification_delivery_attempts",
                newName: "notification_channel_id");

            migrationBuilder.AlterColumn<string>(
                name: "payload_hash",
                table: "sync_items",
                type: "char(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "operation_type",
                table: "sync_items",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "entity_name",
                table: "sync_items",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<Guid>(
                name: "client_record_id",
                table: "sync_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sync_item_id",
                table: "sync_conflicts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "uploaded_item_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "sync_batches",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "downloaded_item_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "conflict_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_refund_amount",
                table: "sales_returns",
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
                table: "sales_returns",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "total_return_amount",
                table: "sales_returns",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_id",
                table: "sales_return_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "sequence_number",
                table: "sales_return_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_refunds",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "requested_amount",
                table: "sales_refunds",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "sales_refunds",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "allocated_amount",
                table: "sales_refund_payment_allocations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_line_id",
                table: "sales_refund_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "sales_refund_lines",
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
                table: "sales_payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "requested_amount",
                table: "sales_payments",
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
                name: "paid_amount",
                table: "sales_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "sales_payment_transactions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "sales_payment_transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<decimal>(
                name: "additional_amount",
                table: "sales_exchanges",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "sales_exchanges",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "replacement_order_id",
                table: "sales_exchanges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_return_line_id",
                table: "sales_exchange_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "replacement_quantity",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "sequence_number",
                table: "sales_exchange_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "applies_to",
                table: "return_reasons",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "return_reasons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "return_reasons",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "inspected_quantity",
                table: "return_inspections",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "inspection_number",
                table: "return_inspections",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "currency_code",
                table: "platform_integrations",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "platform_integrations",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_integration_id",
                table: "platform_integration_webhook_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "platform_integration_webhook_events",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "external_event_id",
                table: "platform_integration_webhook_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "varchar(180)",
                oldMaxLength: 180,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_integration_id",
                table: "platform_integration_request_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "duration_ms",
                table: "platform_integration_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "platform_integration_request_logs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_integration_credentials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "credential_name",
                table: "platform_integration_credentials",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "method_code",
                table: "payment_methods",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "payment_methods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "payment_methods",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "range_start",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "range_end",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "padding_length_snapshot",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<int>(
                name: "next_value",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<Guid>(
                name: "document_number_sequence_id",
                table: "offline_number_blocks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entity_name",
                table: "offline_id_mappings",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_from_sync_item_id",
                table: "offline_id_mappings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "client_record_id",
                table: "offline_id_mappings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<Guid>(
                name: "pos_device_id",
                table: "offline_clients",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "offline_clients",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "max_offline_duration_minutes",
                table: "offline_clients",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "offline_clients",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "notification_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "template_code",
                table: "notification_templates",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                table: "notification_templates",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "notification_templates",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "notification_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "notification_preferences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_user_id",
                table: "notification_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "notification_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "notification_template_version_id",
                table: "notification_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "notification_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "notification_event_types",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_code",
                table: "notification_event_types",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "default_priority",
                table: "notification_event_types",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "notification_event_types",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "notification_event_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "notification_channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "channel_code",
                table: "notification_channels",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(90)",
                oldMaxLength: 90);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "notification_channels",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "platform_integration_id",
                table: "notification_channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "integration_providers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "integration_providers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "last_server_version",
                table: "device_sync_states",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "last_client_version",
                table: "device_sync_states",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_return_amount",
                table: "sales_returns",
                sql: "total_return_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_lines_requested_quantity",
                table: "sales_return_lines",
                sql: "requested_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_sales_return_events_sales_return_id_sequence_number",
                table: "sales_return_events",
                columns: new[] { "sales_return_id", "sequence_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_return_events_sequence_number",
                table: "sales_return_events",
                sql: "sequence_number > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_refunded_amount",
                table: "sales_refunds",
                sql: "refunded_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_requested_amount",
                table: "sales_refunds",
                sql: "requested_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_amount",
                table: "sales_refund_lines",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_requested_amount",
                table: "sales_payments",
                sql: "requested_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_transactions_idempotency_key",
                table: "sales_payment_transactions",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_transactions_amount",
                table: "sales_payment_transactions",
                sql: "amount >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payment_events_sales_payment_id_sequence_number",
                table: "sales_payment_events",
                columns: new[] { "sales_payment_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_replacement_order_id",
                table: "sales_exchanges",
                column: "replacement_order_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchanges_additional_amount",
                table: "sales_exchanges",
                sql: "additional_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchanges_refund_amount",
                table: "sales_exchanges",
                sql: "refund_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchange_lines_replacement_quantity",
                table: "sales_exchange_lines",
                sql: "replacement_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchange_lines_returned_quantity",
                table: "sales_exchange_lines",
                sql: "returned_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_sales_exchange_events_sales_exchange_id_sequence_number",
                table: "sales_exchange_events",
                columns: new[] { "sales_exchange_id", "sequence_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchange_events_sequence_number",
                table: "sales_exchange_events",
                sql: "sequence_number > 0");

            migrationBuilder.CreateIndex(
                name: "uq_return_inspections_tenant_id_inspection_number",
                table: "return_inspections",
                columns: new[] { "tenant_id", "inspection_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_inspections_inspected_quantity",
                table: "return_inspections",
                sql: "inspected_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_tenant_id_notification_event_type_id_channel_type_locale_template_code",
                table: "notification_templates",
                columns: new[] { "tenant_id", "notification_event_type_id", "channel_type", "locale", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_template_versions_notification_template_id",
                table: "notification_template_versions",
                column: "notification_template_id",
                unique: true,
                filter: "is_active_version = true");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_read_receipts_read_source",
                table: "notification_read_receipts",
                sql: "read_source IN ('WEB', 'MOBILE', 'POS', 'ADMIN', 'API')");

            migrationBuilder.CreateIndex(
                name: "uq_notification_events_tenant_id_idempotency_key",
                table: "notification_events",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_notification_channel_id",
                table: "notification_delivery_attempts",
                column: "notification_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_platform_integration_id",
                table: "notification_channels",
                column: "platform_integration_id");

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_platform_integration_id_platform_integrations",
                table: "notification_channels",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_delivery_attempts_notification_channel_id_notification_channels",
                table: "notification_delivery_attempts",
                column: "notification_channel_id",
                principalTable: "notification_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_replacement_order_id_sales_orders",
                table: "sales_exchanges",
                column: "replacement_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
