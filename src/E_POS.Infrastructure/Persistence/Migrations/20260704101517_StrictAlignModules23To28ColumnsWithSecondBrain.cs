using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrictAlignModules23To28ColumnsWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_device_sync_states_offline_client_id_offline_clients",
                table: "device_sync_states");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_fulfillment_method_id_fulfillment_methods",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_outlet_id_outlets",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_events_fulfillment_order_id_fulfillment_orders",
                table: "fulfillment_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_fulfillment_order_id_fulfillment_orders",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_sales_order_line_id_sales_order_lines",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_fulfillment_method_outlet_id_fulfillment_method_outlets",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_sales_order_id_sales_orders",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_delivery_attempts_notification_message_id_notification_messages",
                table: "notification_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_events_notification_event_type_id_notification_event_types",
                table: "notification_events");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_inbox_items_notification_message_id_notification_messages",
                table: "notification_inbox_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_notification_channel_id_notification_channels",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_notification_event_id_notification_events",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_notification_template_version_id_notification_template_versions",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_customer_id_customers",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_notification_event_type_id_notification_event_types",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_platform_user_id_platform_users",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_tenant_user_id_tenant_users",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_notification_inbox_item_id_notification_inbox_items",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_notification_message_id_notification_messages",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_template_versions_notification_template_id_notification_templates",
                table: "notification_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_notification_event_type_id_notification_event_types",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_outlet_id_outlets",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_pos_device_id_pos_devices",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_tenant_id_tenants",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_id_mappings_created_from_sync_item_id_sync_items",
                table: "offline_id_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_id_mappings_offline_client_id_offline_clients",
                table: "offline_id_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_number_blocks_document_number_sequence_id_document_number_sequences",
                table: "offline_number_blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_number_blocks_offline_client_id_offline_clients",
                table: "offline_number_blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_order_events_pickup_order_id_pickup_orders",
                table: "pickup_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_fulfillment_order_id_fulfillment_orders",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_pickup_slot_reservation_id_pickup_slot_reservations",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slot_reservations_pickup_slot_id_pickup_slots",
                table: "pickup_slot_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slots_fulfillment_method_outlet_id_fulfillment_method_outlets",
                table: "pickup_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_credentials_created_by_platform_user_id_platform_users",
                table: "platform_integration_credentials");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_credentials_platform_integration_id_platform_integrations",
                table: "platform_integration_credentials");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_request_logs_integration_provider_id_integration_providers",
                table: "platform_integration_request_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_request_logs_platform_integration_id_platform_integrations",
                table: "platform_integration_request_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_webhook_events_integration_provider_id_integration_providers",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_webhook_events_platform_integration_id_platform_integrations",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integrations_created_by_platform_user_id_platform_users",
                table: "platform_integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integrations_integration_provider_id_integration_providers",
                table: "platform_integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_return_inspections_sales_return_line_id_sales_return_lines",
                table: "return_inspections");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_events_sales_exchange_id_sales_exchanges",
                table: "sales_exchange_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_sales_exchange_id_sales_exchanges",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_sales_return_line_id_sales_return_lines",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_replacement_sales_order_id_sales_orders",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_sales_return_id_sales_returns",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_events_sales_payment_id_sales_payments",
                table: "sales_payment_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_parent_transaction_id_sales_payment_transactions",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_sales_payment_id_sales_payments",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_payment_method_id_payment_methods",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_sales_order_id_sales_orders",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_lines_sales_refund_id_sales_refunds",
                table: "sales_refund_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_lines_sales_return_line_id_sales_return_lines",
                table: "sales_refund_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_original_sales_payment_id_sales_payments",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_refund_payment_method_id_payment_methods",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_sales_refund_id_sales_refunds",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_sales_order_id_sales_orders",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_sales_return_id_sales_returns",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_events_sales_return_id_sales_returns",
                table: "sales_return_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_return_reason_id_return_reasons",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_sales_order_line_id_sales_order_lines",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_sales_return_id_sales_returns",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_return_reason_id_return_reasons",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_sales_order_id_sales_orders",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_batches_offline_client_id_offline_clients",
                table: "sync_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_offline_client_id_offline_clients",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_resolved_by_tenant_user_id_tenant_users",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_sync_batch_id_sync_batches",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_sync_item_id_sync_items",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_items_offline_client_id_offline_clients",
                table: "sync_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_items_sync_batch_id_sync_batches",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "IX_sync_items_offline_client_id",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "IX_sync_items_sync_batch_id",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "uq_sync_items_tenant_id_offline_client_id_entity_name_client_record_id_operation_type_payload_hash",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "IX_sync_conflicts_offline_client_id",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sync_conflicts_resolved_by_tenant_user_id",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sync_conflicts_sync_batch_id",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sync_conflicts_sync_item_id",
                table: "sync_conflicts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_conflicts_resolution_status",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sync_batches_offline_client_id",
                table: "sync_batches");

            migrationBuilder.DropIndex(
                name: "uq_sync_batches_tenant_id_offline_client_id_idempotency_key",
                table: "sync_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_batches_conflict_count",
                table: "sync_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_batches_downloaded_item_count",
                table: "sync_batches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sync_batches_uploaded_item_count",
                table: "sync_batches");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_return_reason_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_sales_order_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "uq_sales_returns_tenant_id_return_number",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_exchange_amount",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_received_qty",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_refund_amount",
                table: "sales_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_returns_total_requested_qty",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_lines_return_reason_id",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_lines_sales_order_line_id",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_lines_sales_return_id",
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

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_sales_order_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_sales_return_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "uq_sales_refunds_tenant_id_refund_number",
                table: "sales_refunds");

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
                name: "IX_sales_refund_payment_allocations_original_sales_payment_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_refund_payment_method_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_sales_refund_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_payment_allocations_allocated_amount",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_payment_allocations_allocation_status",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_lines_sales_refund_id",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_lines_sales_return_line_id",
                table: "sales_refund_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_amount",
                table: "sales_refund_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_refund_lines_quantity",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_payment_method_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_sales_order_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "uq_sales_payments_tenant_id_idempotency_key",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "uq_sales_payments_tenant_id_payment_number",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_change_amount",
                table: "sales_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payments_paid_amount",
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
                name: "IX_sales_payment_transactions_sales_payment_id",
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

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_payment_events_sequence_number",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_replacement_sales_order_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_sales_return_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "uq_sales_exchanges_tenant_id_exchange_number",
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

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_sales_exchange_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_sales_return_line_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_exchange_lines_quantity",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_events_sales_exchange_id",
                table: "sales_exchange_events");

            migrationBuilder.DropIndex(
                name: "uq_return_reasons_tenant_id_reason_code",
                table: "return_reasons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_reasons_applies_to",
                table: "return_reasons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_reasons_sort_order",
                table: "return_reasons");

            migrationBuilder.DropIndex(
                name: "IX_return_inspections_sales_return_line_id",
                table: "return_inspections");

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
                name: "IX_platform_integrations_integration_provider_id",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "uq_platform_integrations_integration_code",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_webhook_events_platform_integration_id",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "uq_platform_webhook_events_provider_external_event",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "uq_platform_webhook_events_provider_idempotency_key",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_request_logs_integration_provider_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_request_logs_platform_integration_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_integration_request_logs_duration_ms",
                table: "platform_integration_request_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_integration_request_logs_response_status_code",
                table: "platform_integration_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_credentials_created_by_platform_user_id",
                table: "platform_integration_credentials");

            migrationBuilder.DropIndex(
                name: "uq_platform_integration_credentials_platform_integration_id_credential_name",
                table: "platform_integration_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_integration_credentials_revoked_at_created_at",
                table: "platform_integration_credentials");

            migrationBuilder.DropIndex(
                name: "uq_pickup_slots_fulfillment_method_outlet_id_slot_date_window_start_window_end",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_capacity",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_reserved_count",
                table: "pickup_slots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slots_reserved_count_capacity",
                table: "pickup_slots");

            migrationBuilder.DropIndex(
                name: "IX_pickup_slot_reservations_pickup_slot_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slot_reservations_checkout_session_id_sales_order_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_slot_reservations_reserved_capacity",
                table: "pickup_slot_reservations");

            migrationBuilder.DropIndex(
                name: "IX_pickup_orders_fulfillment_order_id",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "IX_pickup_orders_pickup_slot_reservation_id",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "uq_pickup_orders_tenant_id_pickup_number",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "uq_pickup_order_events_pickup_order_id_sequence_number",
                table: "pickup_order_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pickup_order_events_sequence_number",
                table: "pickup_order_events");

            migrationBuilder.DropIndex(
                name: "uq_payment_methods_tenant_id_method_code",
                table: "payment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_method_type",
                table: "payment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_sort_order",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "IX_offline_number_blocks_document_number_sequence_id",
                table: "offline_number_blocks");

            migrationBuilder.DropIndex(
                name: "IX_offline_number_blocks_offline_client_id",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_next_value_range_end",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_next_value_range_start",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_padding_length_snapshot",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_range_end_range_start",
                table: "offline_number_blocks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_number_blocks_range_start",
                table: "offline_number_blocks");

            migrationBuilder.DropIndex(
                name: "IX_offline_id_mappings_created_from_sync_item_id",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "IX_offline_id_mappings_offline_client_id",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "uq_offline_id_mappings_tenant_id_entity_name_server_record_id",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "uq_offline_id_mappings_tenant_id_offline_client_id_entity_name_client_record_id",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "IX_offline_clients_outlet_id",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "IX_offline_clients_pos_device_id",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "uq_offline_clients_tenant_id_client_code",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "uq_offline_clients_tenant_id_pos_device_id",
                table: "offline_clients");

            migrationBuilder.DropCheckConstraint(
                name: "ck_offline_clients_max_offline_duration_minutes",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_notification_event_type_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "uq_notification_templates_tenant_id_template_code",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "uq_notification_template_versions_notification_template_id_version_number",
                table: "notification_template_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_template_versions_version_number",
                table: "notification_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_notification_inbox_item_id",
                table: "notification_read_receipts");

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
                name: "IX_notification_preferences_notification_event_type_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_preferences_platform_user_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_preferences_tenant_user_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "uq_notification_preferences_tenant_id_recipient_type_platform_user_id_tenant_user_id_customer_id_notification_event_type_id_channel_type",
                table: "notification_preferences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_preferences_recipient_type_platform_user_id_te~",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_messages_notification_channel_id",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "IX_notification_messages_notification_event_id",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "IX_notification_messages_notification_template_version_id",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "uq_notification_messages_tenant_id_message_number",
                table: "notification_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_messages_recipient_type_platform_user_id_tenan~",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "uq_notification_inbox_items_notification_message_id",
                table: "notification_inbox_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_inbox_items_inbox_status",
                table: "notification_inbox_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_inbox_items_recipient_type_platform_user_id_te~",
                table: "notification_inbox_items");

            migrationBuilder.DropIndex(
                name: "IX_notification_events_notification_event_type_id",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "uq_notification_events_tenant_id_event_number",
                table: "notification_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_events_priority",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "uq_notification_event_types_tenant_id_event_code",
                table: "notification_event_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_event_types_default_priority",
                table: "notification_event_types");

            migrationBuilder.DropIndex(
                name: "uq_notification_delivery_attempts_notification_message_id_attempt_number",
                table: "notification_delivery_attempts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_delivery_attempts_attempt_number",
                table: "notification_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "uq_notification_channels_tenant_id_channel_code",
                table: "notification_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_channels_channel_type",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "uq_integration_providers_provider_code",
                table: "integration_providers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_integration_providers_provider_category",
                table: "integration_providers");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_fulfillment_method_outlet_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_sales_order_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "uq_fulfillment_orders_tenant_id_fulfillment_number",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_fulfillment_order_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_lines_requested_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "uq_fulfillment_order_events_fulfillment_order_id_sequence_number",
                table: "fulfillment_order_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_order_events_sequence_number",
                table: "fulfillment_order_events");

            migrationBuilder.DropIndex(
                name: "uq_fulfillment_methods_tenant_id_method_code",
                table: "fulfillment_methods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fulfillment_methods_method_type",
                table: "fulfillment_methods");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_method_outlets_outlet_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "uq_fulfillment_method_outlets_fulfillment_method_id_outlet_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "IX_device_sync_states_offline_client_id",
                table: "device_sync_states");

            migrationBuilder.DropIndex(
                name: "uq_device_sync_states_tenant_id_offline_client_id_dataset_name",
                table: "device_sync_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_device_sync_states_last_client_version",
                table: "device_sync_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_device_sync_states_last_server_version",
                table: "device_sync_states");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sync_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sync_batches");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sales_return_events");

            migrationBuilder.DropColumn(
                name: "status",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sales_refund_lines");

            migrationBuilder.DropColumn(
                name: "status",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sales_payment_transactions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sales_payment_events");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sales_exchange_events");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "return_inspections");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "platform_integration_request_logs");

            migrationBuilder.DropColumn(
                name: "status",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "offline_id_mappings");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_read_receipts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_inbox_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_events");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "status",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "name",
                table: "fulfillment_methods");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "sales_returns",
                newName: "return_status");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "pickup_order_events",
                newName: "event_at");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "fulfillment_order_events",
                newName: "event_at");

            migrationBuilder.AlterColumn<string>(
                name: "operation_type",
                table: "sync_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "item_status",
                table: "sync_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "direction",
                table: "sync_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "resolution_strategy",
                table: "sync_conflicts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "resolution_status",
                table: "sync_conflicts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "conflict_type",
                table: "sync_conflicts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "uploaded_item_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "sync_type",
                table: "sync_batches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "sync_status",
                table: "sync_batches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

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

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_return_lines",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_tax_amount_snapshot",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "line_tax_amount",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "refund_mode",
                table: "sales_refunds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "approved_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_refunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_status",
                table: "sales_refunds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "sales_refunds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "allocation_status",
                table: "sales_refund_payment_allocations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "refund_line_type",
                table: "sales_refund_lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "paid_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "change_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "sales_payments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "sales_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "sales_payment_transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "transaction_status",
                table: "sales_payment_transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "old_status",
                table: "sales_payment_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "new_status",
                table: "sales_payment_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                table: "sales_payment_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_exchanges",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "refund_back_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "price_difference_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "additional_payment_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_exchange_lines",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "replacement_line_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "original_line_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "net_difference_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "return_reasons",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "return_reasons",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "requires_inspection",
                table: "return_reasons",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "return_reasons",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "integration_status",
                table: "platform_integrations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "integration_category",
                table: "platform_integrations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "environment",
                table: "platform_integrations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.Sql("ALTER TABLE platform_integration_webhook_events ALTER COLUMN source_ip TYPE inet USING NULLIF(source_ip, '')::inet;");

            migrationBuilder.AlterColumn<string>(
                name: "event_status",
                table: "platform_integration_webhook_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "request_status",
                table: "platform_integration_request_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "request_direction",
                table: "platform_integration_request_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "platform_integration_credentials",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "credential_type",
                table: "platform_integration_credentials",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "window_start",
                table: "pickup_slots",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "window_end",
                table: "pickup_slots",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "pickup_slots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "slot_code",
                table: "pickup_slots",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "slot_status",
                table: "pickup_slots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "pickup_slots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "pickup_slot_reservations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "checkout_session_id",
                table: "pickup_slot_reservations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "confirmed_at",
                table: "pickup_slot_reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "pickup_slot_reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "release_reason",
                table: "pickup_slot_reservations",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "released_at",
                table: "pickup_slot_reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reservation_status",
                table: "pickup_slot_reservations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "pickup_slot_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "pickup_orders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "pickup_slot_reservation_id",
                table: "pickup_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "collected_at",
                table: "pickup_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "pickup_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_channel",
                table: "pickup_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_email",
                table: "pickup_orders",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_name",
                table: "pickup_orders",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_phone",
                table: "pickup_orders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_note",
                table: "pickup_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pickup_qr_expires_at",
                table: "pickup_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_qr_token_hash",
                table: "pickup_orders",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pickup_qr_version",
                table: "pickup_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_status",
                table: "pickup_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "verification_method",
                table: "pickup_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "pickup_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_tenant_user_id",
                table: "pickup_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_by_tenant_user_id",
                table: "pickup_order_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_note",
                table: "pickup_order_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_payload_json",
                table: "pickup_order_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "pickup_order_events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "pickup_order_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "pickup_order_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "pickup_order_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<bool>(
                name: "supports_refund",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payment_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "payment_methods",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "requires_reference",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "requires_manual_confirmation",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "method_type",
                table: "payment_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active_for_pos",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_active_for_online",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "allows_change",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "payment_methods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "payment_methods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "padding_length_snapshot",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<string>(
                name: "document_type",
                table: "offline_number_blocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "block_status",
                table: "offline_number_blocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "offline_clients",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "offline_type",
                table: "offline_clients",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<bool>(
                name: "offline_enabled",
                table: "offline_clients",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_templates",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "template_scope",
                table: "notification_templates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_templates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_templates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "notification_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_platform_user_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_tenant_user_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "notification_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_template_versions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "notification_template_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "notification_template_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_read_receipts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_preferences",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<bool>(
                name: "is_enabled",
                table: "notification_preferences",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_preferences",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "muted_until",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "notification_preferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_messages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "priority",
                table: "notification_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "message_type",
                table: "notification_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "message_status",
                table: "notification_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_inbox_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "inbox_status",
                table: "notification_inbox_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "priority",
                table: "notification_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "event_status",
                table: "notification_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_event_types",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_event_types",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "default_priority",
                table: "notification_event_types",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "notification_event_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "notification_event_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "notification_event_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "notification_event_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_delivery_attempts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_channels",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_channels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_channels",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "notification_channels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_platform_user_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_tenant_user_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "notification_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "integration_providers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "provider_type",
                table: "integration_providers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "provider_category",
                table: "integration_providers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "auth_type",
                table: "integration_providers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_to_tenant_user_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "fulfillment_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "fulfillment_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fulfilled_at",
                table: "fulfillment_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_note",
                table: "fulfillment_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_status",
                table: "fulfillment_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "packed_at",
                table: "fulfillment_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "picked_at",
                table: "fulfillment_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ready_at",
                table: "fulfillment_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "requested_fulfillment_date",
                table: "fulfillment_orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_at",
                table: "fulfillment_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_inventory_location_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cancelled_quantity",
                table: "fulfillment_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "fulfilled_quantity",
                table: "fulfillment_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "fulfillment_order_lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "packed_by_tenant_user_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "packed_quantity",
                table: "fulfillment_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "picked_by_tenant_user_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "picked_quantity",
                table: "fulfillment_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_order_line_component_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "event_by_tenant_user_id",
                table: "fulfillment_order_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_note",
                table: "fulfillment_order_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_payload_json",
                table: "fulfillment_order_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "fulfillment_order_events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "fulfillment_order_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "fulfillment_order_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "fulfillment_order_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "fulfillment_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "method_type",
                table: "fulfillment_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "fulfillment_methods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "fulfillment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "method_name",
                table: "fulfillment_methods",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_preparation",
                table: "fulfillment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_slot",
                table: "fulfillment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "fulfillment_methods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "fulfillment_method_outlets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "fulfillment_method_outlets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "fulfillment_method_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "cutoff_time",
                table: "fulfillment_method_outlets",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pickup_window_minutes",
                table: "fulfillment_method_outlets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "preparation_lead_minutes",
                table: "fulfillment_method_outlets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "fulfillment_method_outlets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "fulfillment_method_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sync_direction",
                table: "device_sync_states",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "device_sync_states",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "dataset_name",
                table: "device_sync_states",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "refund_status",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "sales_refunds");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "sales_payments");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "pickup_slots");

            migrationBuilder.DropColumn(
                name: "slot_code",
                table: "pickup_slots");

            migrationBuilder.DropColumn(
                name: "slot_status",
                table: "pickup_slots");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "pickup_slots");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "release_reason",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "released_at",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "reservation_status",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropColumn(
                name: "collected_at",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_contact_channel",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_contact_email",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_contact_name",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_contact_phone",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_note",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_qr_expires_at",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_qr_token_hash",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_qr_version",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_status",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "verification_method",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "verified_by_tenant_user_id",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "event_by_tenant_user_id",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "event_note",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "event_payload_json",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "pickup_order_events");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "archived_by_platform_user_id",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "archived_by_tenant_user_id",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "notification_templates");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "notification_template_versions");

            migrationBuilder.DropColumn(
                name: "muted_until",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "status",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "notification_event_types");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "archived_by_platform_user_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "archived_by_tenant_user_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "notification_channels");

            migrationBuilder.DropColumn(
                name: "assigned_to_tenant_user_id",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "fulfilled_at",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_note",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_status",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "packed_at",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "picked_at",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "ready_at",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "requested_fulfillment_date",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "source_inventory_location_id",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "fulfilled_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "packed_by_tenant_user_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "packed_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "picked_by_tenant_user_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "picked_quantity",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "sales_order_line_component_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "event_by_tenant_user_id",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "event_note",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "event_payload_json",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "fulfillment_order_events");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "fulfillment_methods");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "fulfillment_methods");

            migrationBuilder.DropColumn(
                name: "method_name",
                table: "fulfillment_methods");

            migrationBuilder.DropColumn(
                name: "requires_preparation",
                table: "fulfillment_methods");

            migrationBuilder.DropColumn(
                name: "requires_slot",
                table: "fulfillment_methods");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "fulfillment_methods");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropColumn(
                name: "cutoff_time",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropColumn(
                name: "pickup_window_minutes",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropColumn(
                name: "preparation_lead_minutes",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.RenameColumn(
                name: "return_status",
                table: "sales_returns",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "event_at",
                table: "pickup_order_events",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "event_at",
                table: "fulfillment_order_events",
                newName: "updated_at");

            migrationBuilder.AlterColumn<string>(
                name: "operation_type",
                table: "sync_items",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "item_status",
                table: "sync_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "direction",
                table: "sync_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sync_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "resolution_strategy",
                table: "sync_conflicts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "resolution_status",
                table: "sync_conflicts",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "conflict_type",
                table: "sync_conflicts",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "uploaded_item_count",
                table: "sync_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "sync_type",
                table: "sync_batches",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "sync_status",
                table: "sync_batches",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sync_batches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_returns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_return_lines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_tax_amount_snapshot",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "line_tax_amount",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_return_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "refund_mode",
                table: "sales_refunds",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "approved_amount",
                table: "sales_refunds",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "sales_refunds",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "allocation_status",
                table: "sales_refund_payment_allocations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "refund_line_type",
                table: "sales_refund_lines",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_refund_lines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<decimal>(
                name: "refunded_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "paid_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "change_amount",
                table: "sales_payments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "sales_payments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "sales_payment_transactions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_status",
                table: "sales_payment_transactions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_payment_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "old_status",
                table: "sales_payment_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "new_status",
                table: "sales_payment_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                table: "sales_payment_events",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_payment_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_exchanges",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "refund_back_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "price_difference_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "additional_payment_amount",
                table: "sales_exchanges",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_exchange_lines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "replacement_line_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "original_line_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "net_difference_amount",
                table: "sales_exchange_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "sales_exchange_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "return_reasons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "return_reasons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "requires_inspection",
                table: "return_reasons",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "return_reasons",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "return_inspections",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "integration_status",
                table: "platform_integrations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "integration_category",
                table: "platform_integrations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "environment",
                table: "platform_integrations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql("ALTER TABLE platform_integration_webhook_events ALTER COLUMN source_ip TYPE varchar(45) USING source_ip::text;");

            migrationBuilder.AlterColumn<string>(
                name: "event_status",
                table: "platform_integration_webhook_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "platform_integration_webhook_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "request_status",
                table: "platform_integration_request_logs",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "request_direction",
                table: "platform_integration_request_logs",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "platform_integration_request_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "platform_integration_credentials",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "credential_type",
                table: "platform_integration_credentials",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "window_start",
                table: "pickup_slots",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "window_end",
                table: "pickup_slots",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "pickup_slot_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "checkout_session_id",
                table: "pickup_slot_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "pickup_slot_reservations",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "pickup_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "pickup_slot_reservation_id",
                table: "pickup_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "pickup_orders",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "supports_refund",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "payment_methods",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "payment_methods",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "requires_reference",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "requires_manual_confirmation",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "method_type",
                table: "payment_methods",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active_for_pos",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active_for_online",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "allows_change",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "padding_length_snapshot",
                table: "offline_number_blocks",
                type: "integer",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "document_type",
                table: "offline_number_blocks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "block_status",
                table: "offline_number_blocks",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "offline_id_mappings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "offline_clients",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "offline_type",
                table: "offline_clients",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "offline_enabled",
                table: "offline_clients",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_templates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "template_scope",
                table: "notification_templates",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_templates",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_template_versions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_template_versions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_read_receipts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "notification_read_receipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_read_receipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_preferences",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_enabled",
                table: "notification_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_preferences",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_messages",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "priority",
                table: "notification_messages",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "message_type",
                table: "notification_messages",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "message_status",
                table: "notification_messages",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_messages",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "recipient_type",
                table: "notification_inbox_items",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "inbox_status",
                table: "notification_inbox_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_inbox_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "priority",
                table: "notification_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "event_status",
                table: "notification_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_event_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_event_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "default_priority",
                table: "notification_event_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_delivery_attempts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "notification_delivery_attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_delivery_attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notification_channels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "notification_channels",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "channel_type",
                table: "notification_channels",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "integration_providers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider_type",
                table: "integration_providers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider_category",
                table: "integration_providers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "auth_type",
                table: "integration_providers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "fulfillment_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "fulfillment_orders",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "fulfillment_methods",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "method_type",
                table: "fulfillment_methods",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "fulfillment_methods",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "fulfillment_method_outlets",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "fulfillment_method_outlets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "sync_direction",
                table: "device_sync_states",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "device_sync_states",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "dataset_name",
                table: "device_sync_states",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_sync_items_offline_client_id",
                table: "sync_items",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_items_sync_batch_id",
                table: "sync_items",
                column: "sync_batch_id");

            migrationBuilder.CreateIndex(
                name: "uq_sync_items_tenant_id_offline_client_id_entity_name_client_record_id_operation_type_payload_hash",
                table: "sync_items",
                columns: new[] { "tenant_id", "offline_client_id", "entity_name", "client_record_id", "operation_type", "payload_hash" },
                unique: true,
                filter: "payload_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_offline_client_id",
                table: "sync_conflicts",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_resolved_by_tenant_user_id",
                table: "sync_conflicts",
                column: "resolved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_sync_batch_id",
                table: "sync_conflicts",
                column: "sync_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_sync_item_id",
                table: "sync_conflicts",
                column: "sync_item_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_conflicts_resolution_status",
                table: "sync_conflicts",
                sql: "resolution_status IN ('OPEN', 'RESOLVED', 'IGNORED', 'FAILED')");

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_offline_client_id",
                table: "sync_batches",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "uq_sync_batches_tenant_id_offline_client_id_idempotency_key",
                table: "sync_batches",
                columns: new[] { "tenant_id", "offline_client_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_batches_conflict_count",
                table: "sync_batches",
                sql: "conflict_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_batches_downloaded_item_count",
                table: "sync_batches",
                sql: "downloaded_item_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sync_batches_uploaded_item_count",
                table: "sync_batches",
                sql: "uploaded_item_count >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_return_reason_id",
                table: "sales_returns",
                column: "return_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_sales_order_id",
                table: "sales_returns",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_returns_tenant_id_return_number",
                table: "sales_returns",
                columns: new[] { "tenant_id", "return_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_exchange_amount",
                table: "sales_returns",
                sql: "total_exchange_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_received_qty",
                table: "sales_returns",
                sql: "total_received_qty >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_refund_amount",
                table: "sales_returns",
                sql: "total_refund_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_returns_total_requested_qty",
                table: "sales_returns",
                sql: "total_requested_qty >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_return_reason_id",
                table: "sales_return_lines",
                column: "return_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_sales_order_line_id",
                table: "sales_return_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_sales_return_id",
                table: "sales_return_lines",
                column: "sales_return_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_sales_order_id",
                table: "sales_refunds",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_sales_return_id",
                table: "sales_refunds",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_refunds_tenant_id_refund_number",
                table: "sales_refunds",
                columns: new[] { "tenant_id", "refund_number" },
                unique: true);

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
                name: "IX_sales_refund_payment_allocations_original_sales_payment_id",
                table: "sales_refund_payment_allocations",
                column: "original_sales_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_refund_payment_method_id",
                table: "sales_refund_payment_allocations",
                column: "refund_payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_sales_refund_id",
                table: "sales_refund_payment_allocations",
                column: "sales_refund_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_payment_allocations_allocated_amount",
                table: "sales_refund_payment_allocations",
                sql: "allocated_amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_payment_allocations_allocation_status",
                table: "sales_refund_payment_allocations",
                sql: "allocation_status IN ('PENDING', 'SUCCESS', 'FAILED')");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_sales_refund_id",
                table: "sales_refund_lines",
                column: "sales_refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_sales_return_line_id",
                table: "sales_refund_lines",
                column: "sales_return_line_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_amount",
                table: "sales_refund_lines",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_refund_lines_quantity",
                table: "sales_refund_lines",
                sql: "quantity IS NULL OR quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_payment_method_id",
                table: "sales_payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_sales_order_id",
                table: "sales_payments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payments_tenant_id_idempotency_key",
                table: "sales_payments",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_sales_payments_tenant_id_payment_number",
                table: "sales_payments",
                columns: new[] { "tenant_id", "payment_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_change_amount",
                table: "sales_payments",
                sql: "change_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payments_paid_amount",
                table: "sales_payments",
                sql: "paid_amount >= 0");

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
                name: "IX_sales_payment_transactions_sales_payment_id",
                table: "sales_payment_transactions",
                column: "sales_payment_id");

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_payment_events_sequence_number",
                table: "sales_payment_events",
                sql: "sequence_number > 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_replacement_sales_order_id",
                table: "sales_exchanges",
                column: "replacement_sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_sales_return_id",
                table: "sales_exchanges",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_exchanges_tenant_id_exchange_number",
                table: "sales_exchanges",
                columns: new[] { "tenant_id", "exchange_number" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_sales_exchange_id",
                table: "sales_exchange_lines",
                column: "sales_exchange_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_sales_return_line_id",
                table: "sales_exchange_lines",
                column: "sales_return_line_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_exchange_lines_quantity",
                table: "sales_exchange_lines",
                sql: "quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_events_sales_exchange_id",
                table: "sales_exchange_events",
                column: "sales_exchange_id");

            migrationBuilder.CreateIndex(
                name: "uq_return_reasons_tenant_id_reason_code",
                table: "return_reasons",
                columns: new[] { "tenant_id", "reason_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_reasons_applies_to",
                table: "return_reasons",
                sql: "applies_to IN ('RETURN', 'EXCHANGE', 'BOTH')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_reasons_sort_order",
                table: "return_reasons",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_return_inspections_sales_return_line_id",
                table: "return_inspections",
                column: "sales_return_line_id");

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
                name: "IX_platform_integrations_integration_provider_id",
                table: "platform_integrations",
                column: "integration_provider_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_integrations_integration_code",
                table: "platform_integrations",
                column: "integration_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_webhook_events_platform_integration_id",
                table: "platform_integration_webhook_events",
                column: "platform_integration_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_webhook_events_provider_external_event",
                table: "platform_integration_webhook_events",
                columns: new[] { "integration_provider_id", "external_event_id" },
                unique: true,
                filter: "external_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_platform_webhook_events_provider_idempotency_key",
                table: "platform_integration_webhook_events",
                columns: new[] { "integration_provider_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_integration_provider_id",
                table: "platform_integration_request_logs",
                column: "integration_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_platform_integration_id",
                table: "platform_integration_request_logs",
                column: "platform_integration_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_integration_request_logs_duration_ms",
                table: "platform_integration_request_logs",
                sql: "duration_ms IS NULL OR duration_ms >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_integration_request_logs_response_status_code",
                table: "platform_integration_request_logs",
                sql: "response_status_code IS NULL OR response_status_code >= 100");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_credentials_created_by_platform_user_id",
                table: "platform_integration_credentials",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_integration_credentials_platform_integration_id_credential_name",
                table: "platform_integration_credentials",
                columns: new[] { "platform_integration_id", "credential_name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_integration_credentials_revoked_at_created_at",
                table: "platform_integration_credentials",
                sql: "revoked_at IS NULL OR revoked_at >= created_at");

            migrationBuilder.CreateIndex(
                name: "uq_pickup_slots_fulfillment_method_outlet_id_slot_date_window_start_window_end",
                table: "pickup_slots",
                columns: new[] { "fulfillment_method_outlet_id", "slot_date", "window_start", "window_end" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_capacity",
                table: "pickup_slots",
                sql: "capacity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_reserved_count",
                table: "pickup_slots",
                sql: "reserved_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slots_reserved_count_capacity",
                table: "pickup_slots",
                sql: "reserved_count <= capacity");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slot_reservations_pickup_slot_id",
                table: "pickup_slot_reservations",
                column: "pickup_slot_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slot_reservations_checkout_session_id_sales_order_id",
                table: "pickup_slot_reservations",
                sql: "checkout_session_id IS NOT NULL OR sales_order_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_slot_reservations_reserved_capacity",
                table: "pickup_slot_reservations",
                sql: "reserved_capacity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_fulfillment_order_id",
                table: "pickup_orders",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_pickup_slot_reservation_id",
                table: "pickup_orders",
                column: "pickup_slot_reservation_id");

            migrationBuilder.CreateIndex(
                name: "uq_pickup_orders_tenant_id_pickup_number",
                table: "pickup_orders",
                columns: new[] { "tenant_id", "pickup_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_pickup_order_events_pickup_order_id_sequence_number",
                table: "pickup_order_events",
                columns: new[] { "pickup_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pickup_order_events_sequence_number",
                table: "pickup_order_events",
                sql: "sequence_number > 0");

            migrationBuilder.CreateIndex(
                name: "uq_payment_methods_tenant_id_method_code",
                table: "payment_methods",
                columns: new[] { "tenant_id", "method_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_method_type",
                table: "payment_methods",
                sql: "method_type IN ('CASH', 'CARD', 'QR', 'BANK_TRANSFER', 'MANUAL', 'OTHER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_sort_order",
                table: "payment_methods",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_document_number_sequence_id",
                table: "offline_number_blocks",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_offline_client_id",
                table: "offline_number_blocks",
                column: "offline_client_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_next_value_range_end",
                table: "offline_number_blocks",
                sql: "next_value <= range_end + 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_next_value_range_start",
                table: "offline_number_blocks",
                sql: "next_value >= range_start");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_padding_length_snapshot",
                table: "offline_number_blocks",
                sql: "padding_length_snapshot > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_range_end_range_start",
                table: "offline_number_blocks",
                sql: "range_end >= range_start");

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_number_blocks_range_start",
                table: "offline_number_blocks",
                sql: "range_start > 0");

            migrationBuilder.CreateIndex(
                name: "IX_offline_id_mappings_created_from_sync_item_id",
                table: "offline_id_mappings",
                column: "created_from_sync_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_id_mappings_offline_client_id",
                table: "offline_id_mappings",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "uq_offline_id_mappings_tenant_id_entity_name_server_record_id",
                table: "offline_id_mappings",
                columns: new[] { "tenant_id", "entity_name", "server_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_offline_id_mappings_tenant_id_offline_client_id_entity_name_client_record_id",
                table: "offline_id_mappings",
                columns: new[] { "tenant_id", "offline_client_id", "entity_name", "client_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_outlet_id",
                table: "offline_clients",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_pos_device_id",
                table: "offline_clients",
                column: "pos_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_offline_clients_tenant_id_client_code",
                table: "offline_clients",
                columns: new[] { "tenant_id", "client_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_offline_clients_tenant_id_pos_device_id",
                table: "offline_clients",
                columns: new[] { "tenant_id", "pos_device_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_offline_clients_max_offline_duration_minutes",
                table: "offline_clients",
                sql: "max_offline_duration_minutes IS NULL OR max_offline_duration_minutes > 0");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_notification_event_type_id",
                table: "notification_templates",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_tenant_id_template_code",
                table: "notification_templates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_template_versions_notification_template_id_version_number",
                table: "notification_template_versions",
                columns: new[] { "notification_template_id", "version_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_template_versions_version_number",
                table: "notification_template_versions",
                sql: "version_number > 0");

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_notification_inbox_item_id",
                table: "notification_read_receipts",
                column: "notification_inbox_item_id");

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
                name: "IX_notification_preferences_notification_event_type_id",
                table: "notification_preferences",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_platform_user_id",
                table: "notification_preferences",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_tenant_user_id",
                table: "notification_preferences",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_preferences_tenant_id_recipient_type_platform_user_id_tenant_user_id_customer_id_notification_event_type_id_channel_type",
                table: "notification_preferences",
                columns: new[] { "tenant_id", "recipient_type", "platform_user_id", "tenant_user_id", "customer_id", "notification_event_type_id", "channel_type" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_preferences_recipient_type_platform_user_id_te~",
                table: "notification_preferences",
                sql: "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL AND tenant_user_id IS NULL AND customer_id IS NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL AND platform_user_id IS NULL AND customer_id IS NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL AND platform_user_id IS NULL AND tenant_user_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_notification_channel_id",
                table: "notification_messages",
                column: "notification_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_notification_event_id",
                table: "notification_messages",
                column: "notification_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_notification_template_version_id",
                table: "notification_messages",
                column: "notification_template_version_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_messages_tenant_id_message_number",
                table: "notification_messages",
                columns: new[] { "tenant_id", "message_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_messages_recipient_type_platform_user_id_tenan~",
                table: "notification_messages",
                sql: "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "uq_notification_inbox_items_notification_message_id",
                table: "notification_inbox_items",
                column: "notification_message_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_inbox_items_inbox_status",
                table: "notification_inbox_items",
                sql: "inbox_status IN ('UNREAD', 'READ', 'ARCHIVED', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_inbox_items_recipient_type_platform_user_id_te~",
                table: "notification_inbox_items",
                sql: "(recipient_type = 'PLATFORM_USER' AND platform_user_id IS NOT NULL) OR (recipient_type = 'TENANT_USER' AND tenant_user_id IS NOT NULL) OR (recipient_type = 'CUSTOMER' AND customer_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_notification_event_type_id",
                table: "notification_events",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_events_tenant_id_event_number",
                table: "notification_events",
                columns: new[] { "tenant_id", "event_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_events_priority",
                table: "notification_events",
                sql: "priority IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')");

            migrationBuilder.CreateIndex(
                name: "uq_notification_event_types_tenant_id_event_code",
                table: "notification_event_types",
                columns: new[] { "tenant_id", "event_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_event_types_default_priority",
                table: "notification_event_types",
                sql: "default_priority IN ('LOW', 'NORMAL', 'HIGH', 'URGENT')");

            migrationBuilder.CreateIndex(
                name: "uq_notification_delivery_attempts_notification_message_id_attempt_number",
                table: "notification_delivery_attempts",
                columns: new[] { "notification_message_id", "attempt_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_delivery_attempts_attempt_number",
                table: "notification_delivery_attempts",
                sql: "attempt_number > 0");

            migrationBuilder.CreateIndex(
                name: "uq_notification_channels_tenant_id_channel_code",
                table: "notification_channels",
                columns: new[] { "tenant_id", "channel_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_channels_channel_type",
                table: "notification_channels",
                sql: "channel_type IN ('EMAIL', 'SMS', 'WHATSAPP', 'PUSH', 'IN_APP')");

            migrationBuilder.CreateIndex(
                name: "uq_integration_providers_provider_code",
                table: "integration_providers",
                column: "provider_code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_integration_providers_provider_category",
                table: "integration_providers",
                sql: "provider_category IN ('PAYMENT', 'SMS', 'EMAIL', 'WHATSAPP', 'ACCOUNTING', 'DELIVERY', 'ANALYTICS', 'OTHER')");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_fulfillment_method_outlet_id",
                table: "fulfillment_orders",
                column: "fulfillment_method_outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_sales_order_id",
                table: "fulfillment_orders",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_orders_tenant_id_fulfillment_number",
                table: "fulfillment_orders",
                columns: new[] { "tenant_id", "fulfillment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_fulfillment_order_id",
                table: "fulfillment_order_lines",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_id",
                table: "fulfillment_order_lines",
                column: "sales_order_line_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_lines_requested_quantity",
                table: "fulfillment_order_lines",
                sql: "requested_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_order_events_fulfillment_order_id_sequence_number",
                table: "fulfillment_order_events",
                columns: new[] { "fulfillment_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_order_events_sequence_number",
                table: "fulfillment_order_events",
                sql: "sequence_number > 0");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_methods_tenant_id_method_code",
                table: "fulfillment_methods",
                columns: new[] { "tenant_id", "method_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_fulfillment_methods_method_type",
                table: "fulfillment_methods",
                sql: "method_type IN ('IMMEDIATE', 'PICKUP')");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_method_outlets_outlet_id",
                table: "fulfillment_method_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_method_outlets_fulfillment_method_id_outlet_id",
                table: "fulfillment_method_outlets",
                columns: new[] { "fulfillment_method_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_sync_states_offline_client_id",
                table: "device_sync_states",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "uq_device_sync_states_tenant_id_offline_client_id_dataset_name",
                table: "device_sync_states",
                columns: new[] { "tenant_id", "offline_client_id", "dataset_name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_device_sync_states_last_client_version",
                table: "device_sync_states",
                sql: "last_client_version IS NULL OR last_client_version >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_device_sync_states_last_server_version",
                table: "device_sync_states",
                sql: "last_server_version IS NULL OR last_server_version >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_device_sync_states_offline_client_id_offline_clients",
                table: "device_sync_states",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_fulfillment_method_id_fulfillment_methods",
                table: "fulfillment_method_outlets",
                column: "fulfillment_method_id",
                principalTable: "fulfillment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_outlet_id_outlets",
                table: "fulfillment_method_outlets",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_events_fulfillment_order_id_fulfillment_orders",
                table: "fulfillment_order_events",
                column: "fulfillment_order_id",
                principalTable: "fulfillment_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_fulfillment_order_id_fulfillment_orders",
                table: "fulfillment_order_lines",
                column: "fulfillment_order_id",
                principalTable: "fulfillment_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_sales_order_line_id_sales_order_lines",
                table: "fulfillment_order_lines",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_fulfillment_method_outlet_id_fulfillment_method_outlets",
                table: "fulfillment_orders",
                column: "fulfillment_method_outlet_id",
                principalTable: "fulfillment_method_outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_sales_order_id_sales_orders",
                table: "fulfillment_orders",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_delivery_attempts_notification_message_id_notification_messages",
                table: "notification_delivery_attempts",
                column: "notification_message_id",
                principalTable: "notification_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_events_notification_event_type_id_notification_event_types",
                table: "notification_events",
                column: "notification_event_type_id",
                principalTable: "notification_event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_inbox_items_notification_message_id_notification_messages",
                table: "notification_inbox_items",
                column: "notification_message_id",
                principalTable: "notification_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_notification_channel_id_notification_channels",
                table: "notification_messages",
                column: "notification_channel_id",
                principalTable: "notification_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_notification_event_id_notification_events",
                table: "notification_messages",
                column: "notification_event_id",
                principalTable: "notification_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_notification_template_version_id_notification_template_versions",
                table: "notification_messages",
                column: "notification_template_version_id",
                principalTable: "notification_template_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_customer_id_customers",
                table: "notification_preferences",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_notification_event_type_id_notification_event_types",
                table: "notification_preferences",
                column: "notification_event_type_id",
                principalTable: "notification_event_types",
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
                name: "fk_notification_read_receipts_notification_inbox_item_id_notification_inbox_items",
                table: "notification_read_receipts",
                column: "notification_inbox_item_id",
                principalTable: "notification_inbox_items",
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
                name: "fk_notification_template_versions_notification_template_id_notification_templates",
                table: "notification_template_versions",
                column: "notification_template_id",
                principalTable: "notification_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_notification_event_type_id_notification_event_types",
                table: "notification_templates",
                column: "notification_event_type_id",
                principalTable: "notification_event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_outlet_id_outlets",
                table: "offline_clients",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_pos_device_id_pos_devices",
                table: "offline_clients",
                column: "pos_device_id",
                principalTable: "pos_devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_tenant_id_tenants",
                table: "offline_clients",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_id_mappings_created_from_sync_item_id_sync_items",
                table: "offline_id_mappings",
                column: "created_from_sync_item_id",
                principalTable: "sync_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_id_mappings_offline_client_id_offline_clients",
                table: "offline_id_mappings",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_number_blocks_document_number_sequence_id_document_number_sequences",
                table: "offline_number_blocks",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_number_blocks_offline_client_id_offline_clients",
                table: "offline_number_blocks",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_order_events_pickup_order_id_pickup_orders",
                table: "pickup_order_events",
                column: "pickup_order_id",
                principalTable: "pickup_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_fulfillment_order_id_fulfillment_orders",
                table: "pickup_orders",
                column: "fulfillment_order_id",
                principalTable: "fulfillment_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_pickup_slot_reservation_id_pickup_slot_reservations",
                table: "pickup_orders",
                column: "pickup_slot_reservation_id",
                principalTable: "pickup_slot_reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slot_reservations_pickup_slot_id_pickup_slots",
                table: "pickup_slot_reservations",
                column: "pickup_slot_id",
                principalTable: "pickup_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slots_fulfillment_method_outlet_id_fulfillment_method_outlets",
                table: "pickup_slots",
                column: "fulfillment_method_outlet_id",
                principalTable: "fulfillment_method_outlets",
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
                name: "fk_platform_integration_credentials_platform_integration_id_platform_integrations",
                table: "platform_integration_credentials",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_request_logs_integration_provider_id_integration_providers",
                table: "platform_integration_request_logs",
                column: "integration_provider_id",
                principalTable: "integration_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_request_logs_platform_integration_id_platform_integrations",
                table: "platform_integration_request_logs",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_webhook_events_integration_provider_id_integration_providers",
                table: "platform_integration_webhook_events",
                column: "integration_provider_id",
                principalTable: "integration_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_webhook_events_platform_integration_id_platform_integrations",
                table: "platform_integration_webhook_events",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
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
                name: "fk_platform_integrations_integration_provider_id_integration_providers",
                table: "platform_integrations",
                column: "integration_provider_id",
                principalTable: "integration_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_inspections_sales_return_line_id_sales_return_lines",
                table: "return_inspections",
                column: "sales_return_line_id",
                principalTable: "sales_return_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_events_sales_exchange_id_sales_exchanges",
                table: "sales_exchange_events",
                column: "sales_exchange_id",
                principalTable: "sales_exchanges",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_sales_exchange_id_sales_exchanges",
                table: "sales_exchange_lines",
                column: "sales_exchange_id",
                principalTable: "sales_exchanges",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_sales_return_line_id_sales_return_lines",
                table: "sales_exchange_lines",
                column: "sales_return_line_id",
                principalTable: "sales_return_lines",
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
                name: "fk_sales_exchanges_sales_return_id_sales_returns",
                table: "sales_exchanges",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_events_sales_payment_id_sales_payments",
                table: "sales_payment_events",
                column: "sales_payment_id",
                principalTable: "sales_payments",
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
                name: "fk_sales_payment_transactions_sales_payment_id_sales_payments",
                table: "sales_payment_transactions",
                column: "sales_payment_id",
                principalTable: "sales_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_payment_method_id_payment_methods",
                table: "sales_payments",
                column: "payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_sales_order_id_sales_orders",
                table: "sales_payments",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_lines_sales_refund_id_sales_refunds",
                table: "sales_refund_lines",
                column: "sales_refund_id",
                principalTable: "sales_refunds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_lines_sales_return_line_id_sales_return_lines",
                table: "sales_refund_lines",
                column: "sales_return_line_id",
                principalTable: "sales_return_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_original_sales_payment_id_sales_payments",
                table: "sales_refund_payment_allocations",
                column: "original_sales_payment_id",
                principalTable: "sales_payments",
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
                name: "fk_sales_refund_payment_allocations_sales_refund_id_sales_refunds",
                table: "sales_refund_payment_allocations",
                column: "sales_refund_id",
                principalTable: "sales_refunds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_sales_order_id_sales_orders",
                table: "sales_refunds",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_sales_return_id_sales_returns",
                table: "sales_refunds",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_events_sales_return_id_sales_returns",
                table: "sales_return_events",
                column: "sales_return_id",
                principalTable: "sales_returns",
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
                name: "fk_sales_return_lines_sales_order_line_id_sales_order_lines",
                table: "sales_return_lines",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_sales_return_id_sales_returns",
                table: "sales_return_lines",
                column: "sales_return_id",
                principalTable: "sales_returns",
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
                name: "fk_sales_returns_sales_order_id_sales_orders",
                table: "sales_returns",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_batches_offline_client_id_offline_clients",
                table: "sync_batches",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_offline_client_id_offline_clients",
                table: "sync_conflicts",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_resolved_by_tenant_user_id_tenant_users",
                table: "sync_conflicts",
                column: "resolved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_sync_batch_id_sync_batches",
                table: "sync_conflicts",
                column: "sync_batch_id",
                principalTable: "sync_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_sync_item_id_sync_items",
                table: "sync_conflicts",
                column: "sync_item_id",
                principalTable: "sync_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_items_offline_client_id_offline_clients",
                table: "sync_items",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_items_sync_batch_id_sync_batches",
                table: "sync_items",
                column: "sync_batch_id",
                principalTable: "sync_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
