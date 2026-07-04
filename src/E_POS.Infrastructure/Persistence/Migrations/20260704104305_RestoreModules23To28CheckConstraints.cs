using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreModules23To28CheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_batches_615b7ce4",
                table: "sync_batches",
                sql: "conflict_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_batches_b689bf2f",
                table: "sync_batches",
                sql: "uploaded_item_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_batches_fde4f4bc",
                table: "sync_batches",
                sql: "downloaded_item_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_2b7cb4b2",
                table: "sales_refunds",
                sql: "requested_amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_732d4048",
                table: "sales_refunds",
                sql: "refunded_amount <= approved_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refunds_a3883de0",
                table: "sales_refunds",
                sql: "approved_amount <= requested_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_payment_allocations_93be5f04",
                table: "sales_refund_payment_allocations",
                sql: "allocated_amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_ef7f4598",
                table: "sales_refund_lines",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_f90ed063",
                table: "sales_refund_lines",
                sql: "quantity IS NULL OR quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_5e2a8fc2",
                table: "sales_payments",
                sql: "refunded_amount <= paid_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_e5c553d5",
                table: "sales_payments",
                sql: "requested_amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_transactions_d53c5618",
                table: "sales_payment_transactions",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_events_a1a5b828",
                table: "sales_payment_events",
                sql: "sequence_number > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_integration_request_logs_a2ce2e91",
                table: "platform_integration_request_logs",
                sql: "response_status_code IS NULL OR response_status_code >= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_integration_request_logs_af20525d",
                table: "platform_integration_request_logs",
                sql: "duration_ms IS NULL OR duration_ms >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_222cc075",
                table: "payment_methods",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_19fcb4b9",
                table: "offline_number_blocks",
                sql: "next_value >= range_start");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_52545072",
                table: "offline_number_blocks",
                sql: "range_end >= range_start");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_c7dcd2aa",
                table: "offline_number_blocks",
                sql: "padding_length_snapshot > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_cb2aa85a",
                table: "offline_number_blocks",
                sql: "range_start > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_cc44e5d4",
                table: "offline_number_blocks",
                sql: "next_value <= range_end + 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_clients_09d344aa",
                table: "offline_clients",
                sql: "max_offline_duration_minutes IS NULL OR max_offline_duration_minutes > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_device_sync_states_2920bfd8",
                table: "device_sync_states",
                sql: "last_client_version IS NULL OR last_client_version >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_device_sync_states_cb94ed2e",
                table: "device_sync_states",
                sql: "last_server_version IS NULL OR last_server_version >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_batches_615b7ce4",
                table: "sync_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_batches_b689bf2f",
                table: "sync_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_batches_fde4f4bc",
                table: "sync_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_2b7cb4b2",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_732d4048",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refunds_a3883de0",
                table: "sales_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_payment_allocations_93be5f04",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_ef7f4598",
                table: "sales_refund_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_f90ed063",
                table: "sales_refund_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_5e2a8fc2",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_e5c553d5",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_transactions_d53c5618",
                table: "sales_payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_events_a1a5b828",
                table: "sales_payment_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_integration_request_logs_a2ce2e91",
                table: "platform_integration_request_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_integration_request_logs_af20525d",
                table: "platform_integration_request_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_222cc075",
                table: "payment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_19fcb4b9",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_52545072",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_c7dcd2aa",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_cb2aa85a",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_cc44e5d4",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_clients_09d344aa",
                table: "offline_clients");

            migrationBuilder.DropCheckConstraint(
                name: "ck_device_sync_states_2920bfd8",
                table: "device_sync_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_device_sync_states_cb94ed2e",
                table: "device_sync_states");
        }
    }
}
