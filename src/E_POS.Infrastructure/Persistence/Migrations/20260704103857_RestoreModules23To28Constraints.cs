using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreModules23To28Constraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_sync_items_offline_client_id",
                table: "sync_items",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_items_sync_batch_id",
                table: "sync_items",
                column: "sync_batch_id");

            migrationBuilder.CreateIndex(
                name: "ux_sync_items_ae7f345a",
                table: "sync_items",
                columns: new[] { "tenant_id", "offline_client_id", "entity_name", "client_record_id", "operation_type", "payload_hash" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_sync_conflicts_tenant_id",
                table: "sync_conflicts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_offline_client_id",
                table: "sync_batches",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "ux_sync_batches_a99603d8",
                table: "sync_batches",
                columns: new[] { "tenant_id", "offline_client_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_created_by_tenant_user_id",
                table: "sales_returns",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_customer_id",
                table: "sales_returns",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_document_number_sequence_id",
                table: "sales_returns",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_outlet_id",
                table: "sales_returns",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_return_reason_id",
                table: "sales_returns",
                column: "return_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_sales_order_id",
                table: "sales_returns",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_returns_updated_by_tenant_user_id",
                table: "sales_returns",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_sales_returns_35ae5e87",
                table: "sales_returns",
                columns: new[] { "tenant_id", "return_number" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_lines_tenant_id",
                table: "sales_return_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_events_created_by_tenant_user_id",
                table: "sales_return_events",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_events_sales_return_id",
                table: "sales_return_events",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_return_events_tenant_id",
                table: "sales_return_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_approved_by_tenant_user_id",
                table: "sales_refunds",
                column: "approved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_created_by_tenant_user_id",
                table: "sales_refunds",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_currency_code",
                table: "sales_refunds",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_document_number_sequence_id",
                table: "sales_refunds",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_sales_order_id",
                table: "sales_refunds",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_sales_return_id",
                table: "sales_refunds",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refunds_updated_by_tenant_user_id",
                table: "sales_refunds",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_sales_refunds_bf2fab40",
                table: "sales_refunds",
                columns: new[] { "tenant_id", "refund_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_original_sales_payment_id",
                table: "sales_refund_payment_allocations",
                column: "original_sales_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_refund_payment_method_id",
                table: "sales_refund_payment_allocations",
                column: "refund_payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_refund_transaction_id",
                table: "sales_refund_payment_allocations",
                column: "refund_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_sales_refund_id",
                table: "sales_refund_payment_allocations",
                column: "sales_refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_payment_allocations_tenant_id",
                table: "sales_refund_payment_allocations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_sales_refund_id",
                table: "sales_refund_lines",
                column: "sales_refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_sales_return_line_id",
                table: "sales_refund_lines",
                column: "sales_return_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_refund_lines_tenant_id",
                table: "sales_refund_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_created_by_tenant_user_id",
                table: "sales_payments",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_currency_code",
                table: "sales_payments",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_document_number_sequence_id",
                table: "sales_payments",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_payment_method_id",
                table: "sales_payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_sales_order_id",
                table: "sales_payments",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_till_id",
                table: "sales_payments",
                column: "till_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_till_session_id",
                table: "sales_payments",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payments_updated_by_tenant_user_id",
                table: "sales_payments",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_sales_payments_3aae300c",
                table: "sales_payments",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_sales_payments_805b1537",
                table: "sales_payments",
                columns: new[] { "tenant_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_transactions_parent_transaction_id",
                table: "sales_payment_transactions",
                column: "parent_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_transactions_processed_by_tenant_user_id",
                table: "sales_payment_transactions",
                column: "processed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_transactions_sales_payment_id",
                table: "sales_payment_transactions",
                column: "sales_payment_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_events_event_by_tenant_user_id",
                table: "sales_payment_events",
                column: "event_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_payment_events_sales_payment_id",
                table: "sales_payment_events",
                column: "sales_payment_id");

            migrationBuilder.CreateIndex(
                name: "ux_sales_payment_events_f38768c9",
                table: "sales_payment_events",
                columns: new[] { "tenant_id", "sales_payment_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_created_by_tenant_user_id",
                table: "sales_exchanges",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_document_number_sequence_id",
                table: "sales_exchanges",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_replacement_sales_order_id",
                table: "sales_exchanges",
                column: "replacement_sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_sales_return_id",
                table: "sales_exchanges",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchanges_updated_by_tenant_user_id",
                table: "sales_exchanges",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_sales_exchanges_3413dbba",
                table: "sales_exchanges",
                columns: new[] { "tenant_id", "exchange_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_replacement_product_id",
                table: "sales_exchange_lines",
                column: "replacement_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_replacement_product_variant_id",
                table: "sales_exchange_lines",
                column: "replacement_product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_replacement_sales_order_line_id",
                table: "sales_exchange_lines",
                column: "replacement_sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_sales_exchange_id",
                table: "sales_exchange_lines",
                column: "sales_exchange_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_sales_return_line_id",
                table: "sales_exchange_lines",
                column: "sales_return_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_lines_tenant_id",
                table: "sales_exchange_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_events_created_by_tenant_user_id",
                table: "sales_exchange_events",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_events_sales_exchange_id",
                table: "sales_exchange_events",
                column: "sales_exchange_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_exchange_events_tenant_id",
                table: "sales_exchange_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_return_reasons_978380c7",
                table: "return_reasons",
                columns: new[] { "tenant_id", "reason_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_return_inspections_inspected_by_tenant_user_id",
                table: "return_inspections",
                column: "inspected_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_inspections_inventory_location_id",
                table: "return_inspections",
                column: "inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_inspections_sales_return_line_id",
                table: "return_inspections",
                column: "sales_return_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_inspections_tenant_id",
                table: "return_inspections",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integrations_created_by_platform_user_id",
                table: "platform_integrations",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integrations_currency_code",
                table: "platform_integrations",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integrations_integration_provider_id",
                table: "platform_integrations",
                column: "integration_provider_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_integrations_04f0d1d4",
                table: "platform_integrations",
                column: "integration_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_webhook_events_platform_integration_id",
                table: "platform_integration_webhook_events",
                column: "platform_integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_webhook_events_tenant_id",
                table: "platform_integration_webhook_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_integration_webhook_events_900a6647",
                table: "platform_integration_webhook_events",
                columns: new[] { "integration_provider_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_platform_integration_webhook_events_be2c7f88",
                table: "platform_integration_webhook_events",
                columns: new[] { "integration_provider_id", "external_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_integration_provider_id",
                table: "platform_integration_request_logs",
                column: "integration_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_platform_integration_id",
                table: "platform_integration_request_logs",
                column: "platform_integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_request_logs_tenant_id",
                table: "platform_integration_request_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_integration_credentials_created_by_platform_user_id",
                table: "platform_integration_credentials",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_integration_credentials_f64f2b19",
                table: "platform_integration_credentials",
                columns: new[] { "platform_integration_id", "credential_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slots_fulfillment_method_outlet_id",
                table: "pickup_slots",
                column: "fulfillment_method_outlet_id");

            migrationBuilder.CreateIndex(
                name: "ux_pickup_slots_d08294ab",
                table: "pickup_slots",
                columns: new[] { "tenant_id", "fulfillment_method_outlet_id", "slot_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slot_reservations_checkout_session_id",
                table: "pickup_slot_reservations",
                column: "checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slot_reservations_pickup_slot_id",
                table: "pickup_slot_reservations",
                column: "pickup_slot_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slot_reservations_sales_order_id",
                table: "pickup_slot_reservations",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_slot_reservations_tenant_id",
                table: "pickup_slot_reservations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_document_number_sequence_id",
                table: "pickup_orders",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_fulfillment_order_id",
                table: "pickup_orders",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_pickup_slot_reservation_id",
                table: "pickup_orders",
                column: "pickup_slot_reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_verified_by_tenant_user_id",
                table: "pickup_orders",
                column: "verified_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_pickup_orders_917d8d64",
                table: "pickup_orders",
                columns: new[] { "tenant_id", "pickup_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pickup_orders_9c361648",
                table: "pickup_orders",
                columns: new[] { "tenant_id", "fulfillment_order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_events_event_by_tenant_user_id",
                table: "pickup_order_events",
                column: "event_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_events_pickup_order_id",
                table: "pickup_order_events",
                column: "pickup_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_events_tenant_id",
                table: "pickup_order_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_created_by_tenant_user_id",
                table: "payment_methods",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_methods_updated_by_tenant_user_id",
                table: "payment_methods",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_payment_methods_d1d0cc7d",
                table: "payment_methods",
                columns: new[] { "tenant_id", "method_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_document_number_sequence_id",
                table: "offline_number_blocks",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_offline_client_id",
                table: "offline_number_blocks",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_number_blocks_tenant_id",
                table: "offline_number_blocks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_id_mappings_created_from_sync_item_id",
                table: "offline_id_mappings",
                column: "created_from_sync_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_id_mappings_offline_client_id",
                table: "offline_id_mappings",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "ux_offline_id_mappings_34294dee",
                table: "offline_id_mappings",
                columns: new[] { "tenant_id", "offline_client_id", "entity_name", "client_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_offline_id_mappings_e7802dc9",
                table: "offline_id_mappings",
                columns: new[] { "tenant_id", "entity_name", "server_record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_created_by_tenant_user_id",
                table: "offline_clients",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_outlet_id",
                table: "offline_clients",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_pos_device_id",
                table: "offline_clients",
                column: "pos_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_offline_clients_updated_by_tenant_user_id",
                table: "offline_clients",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_offline_clients_60137369",
                table: "offline_clients",
                columns: new[] { "tenant_id", "client_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_offline_clients_86310fa1",
                table: "offline_clients",
                columns: new[] { "tenant_id", "pos_device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_archived_by_platform_user_id",
                table: "notification_templates",
                column: "archived_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_archived_by_tenant_user_id",
                table: "notification_templates",
                column: "archived_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_created_by_platform_user_id",
                table: "notification_templates",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_created_by_tenant_user_id",
                table: "notification_templates",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_notification_event_type_id",
                table: "notification_templates",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_updated_by_platform_user_id",
                table: "notification_templates",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_updated_by_tenant_user_id",
                table: "notification_templates",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_templates_14448ba8",
                table: "notification_templates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_template_versions_created_by_platform_user_id",
                table: "notification_template_versions",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_template_versions_created_by_tenant_user_id",
                table: "notification_template_versions",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_template_versions_tenant_id",
                table: "notification_template_versions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_template_versions_d19e2b03",
                table: "notification_template_versions",
                columns: new[] { "notification_template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_customer_id",
                table: "notification_read_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_notification_inbox_item_id",
                table: "notification_read_receipts",
                column: "notification_inbox_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_notification_message_id",
                table: "notification_read_receipts",
                column: "notification_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_platform_user_id",
                table: "notification_read_receipts",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_tenant_id",
                table: "notification_read_receipts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_read_receipts_tenant_user_id",
                table: "notification_read_receipts",
                column: "tenant_user_id");

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
                name: "IX_notification_preferences_tenant_id",
                table: "notification_preferences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_tenant_user_id",
                table: "notification_preferences",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_customer_id",
                table: "notification_messages",
                column: "customer_id");

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
                name: "IX_notification_messages_platform_user_id",
                table: "notification_messages",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_messages_tenant_user_id",
                table: "notification_messages",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_messages_02c0854c",
                table: "notification_messages",
                columns: new[] { "tenant_id", "message_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_inbox_items_customer_id",
                table: "notification_inbox_items",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_inbox_items_platform_user_id",
                table: "notification_inbox_items",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_inbox_items_tenant_id",
                table: "notification_inbox_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_inbox_items_tenant_user_id",
                table: "notification_inbox_items",
                column: "tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_inbox_items_5e98743f",
                table: "notification_inbox_items",
                column: "notification_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_created_by_platform_user_id",
                table: "notification_events",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_created_by_tenant_user_id",
                table: "notification_events",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_notification_event_type_id",
                table: "notification_events",
                column: "notification_event_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_events_05be9a99",
                table: "notification_events",
                columns: new[] { "tenant_id", "event_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_event_types_created_by_platform_user_id",
                table: "notification_event_types",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_event_types_created_by_tenant_user_id",
                table: "notification_event_types",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_event_types_updated_by_platform_user_id",
                table: "notification_event_types",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_event_types_updated_by_tenant_user_id",
                table: "notification_event_types",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_event_types_ff3ad4c7",
                table: "notification_event_types",
                columns: new[] { "tenant_id", "event_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_tenant_id",
                table: "notification_delivery_attempts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_delivery_attempts_59643ebe",
                table: "notification_delivery_attempts",
                columns: new[] { "notification_message_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_archived_by_platform_user_id",
                table: "notification_channels",
                column: "archived_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_archived_by_tenant_user_id",
                table: "notification_channels",
                column: "archived_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_created_by_platform_user_id",
                table: "notification_channels",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_created_by_tenant_user_id",
                table: "notification_channels",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_updated_by_platform_user_id",
                table: "notification_channels",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channels_updated_by_tenant_user_id",
                table: "notification_channels",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_channels_5708f727",
                table: "notification_channels",
                columns: new[] { "tenant_id", "channel_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_integration_providers_b845f32a",
                table: "integration_providers",
                column: "provider_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_assigned_to_tenant_user_id",
                table: "fulfillment_orders",
                column: "assigned_to_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_created_by_tenant_user_id",
                table: "fulfillment_orders",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_document_number_sequence_id",
                table: "fulfillment_orders",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_fulfillment_method_outlet_id",
                table: "fulfillment_orders",
                column: "fulfillment_method_outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_sales_order_id",
                table: "fulfillment_orders",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_source_inventory_location_id",
                table: "fulfillment_orders",
                column: "source_inventory_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_orders_updated_by_tenant_user_id",
                table: "fulfillment_orders",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_fulfillment_orders_e767fb12",
                table: "fulfillment_orders",
                columns: new[] { "tenant_id", "fulfillment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_fulfillment_order_id",
                table: "fulfillment_order_lines",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_packed_by_tenant_user_id",
                table: "fulfillment_order_lines",
                column: "packed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_picked_by_tenant_user_id",
                table: "fulfillment_order_lines",
                column: "picked_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_component_id",
                table: "fulfillment_order_lines",
                column: "sales_order_line_component_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_id",
                table: "fulfillment_order_lines",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_tenant_id",
                table: "fulfillment_order_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_events_event_by_tenant_user_id",
                table: "fulfillment_order_events",
                column: "event_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_events_fulfillment_order_id",
                table: "fulfillment_order_events",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_events_tenant_id",
                table: "fulfillment_order_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_methods_created_by_tenant_user_id",
                table: "fulfillment_methods",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_methods_updated_by_tenant_user_id",
                table: "fulfillment_methods",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_fulfillment_methods_4ec69d59",
                table: "fulfillment_methods",
                columns: new[] { "tenant_id", "method_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_method_outlets_created_by_tenant_user_id",
                table: "fulfillment_method_outlets",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_method_outlets_fulfillment_method_id",
                table: "fulfillment_method_outlets",
                column: "fulfillment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_method_outlets_outlet_id",
                table: "fulfillment_method_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_method_outlets_updated_by_tenant_user_id",
                table: "fulfillment_method_outlets",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_fulfillment_method_outlets_cf79fc78",
                table: "fulfillment_method_outlets",
                columns: new[] { "tenant_id", "fulfillment_method_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_sync_states_offline_client_id",
                table: "device_sync_states",
                column: "offline_client_id");

            migrationBuilder.CreateIndex(
                name: "ux_device_sync_states_8060ebb9",
                table: "device_sync_states",
                columns: new[] { "tenant_id", "offline_client_id", "dataset_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_device_sync_states_a0868895",
                table: "device_sync_states",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_device_sync_states_fe00dcdd",
                table: "device_sync_states",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_6480bfac",
                table: "fulfillment_method_outlets",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_821f4b29",
                table: "fulfillment_method_outlets",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_bd06d410",
                table: "fulfillment_method_outlets",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_e627f2fa",
                table: "fulfillment_method_outlets",
                column: "fulfillment_method_id",
                principalTable: "fulfillment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_method_outlets_f359be5f",
                table: "fulfillment_method_outlets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_methods_44d027b0",
                table: "fulfillment_methods",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_methods_6a6a1daa",
                table: "fulfillment_methods",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_methods_87b8bdca",
                table: "fulfillment_methods",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_events_8fff23fa",
                table: "fulfillment_order_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_events_ad8a6f3d",
                table: "fulfillment_order_events",
                column: "event_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_events_beeeae6b",
                table: "fulfillment_order_events",
                column: "fulfillment_order_id",
                principalTable: "fulfillment_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_5ac519a0",
                table: "fulfillment_order_lines",
                column: "fulfillment_order_id",
                principalTable: "fulfillment_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_9a8cfc05",
                table: "fulfillment_order_lines",
                column: "sales_order_line_component_id",
                principalTable: "sales_order_line_components",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_aff467be",
                table: "fulfillment_order_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_bcaca9ef",
                table: "fulfillment_order_lines",
                column: "packed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_ec4817ec",
                table: "fulfillment_order_lines",
                column: "picked_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_f29543cf",
                table: "fulfillment_order_lines",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_0197fb4f",
                table: "fulfillment_orders",
                column: "assigned_to_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_076928b6",
                table: "fulfillment_orders",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_8d547aff",
                table: "fulfillment_orders",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_93e151fe",
                table: "fulfillment_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_b268c630",
                table: "fulfillment_orders",
                column: "source_inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_baf8addd",
                table: "fulfillment_orders",
                column: "fulfillment_method_outlet_id",
                principalTable: "fulfillment_method_outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_d02b8cf5",
                table: "fulfillment_orders",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_orders_dc59b3c6",
                table: "fulfillment_orders",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_068b9801",
                table: "notification_channels",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_0fe25ca8",
                table: "notification_channels",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_6582c928",
                table: "notification_channels",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_6c57bfdf",
                table: "notification_channels",
                column: "archived_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_bd6812f0",
                table: "notification_channels",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_c29ac65e",
                table: "notification_channels",
                column: "archived_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_channels_e57275e8",
                table: "notification_channels",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_delivery_attempts_117f17e1",
                table: "notification_delivery_attempts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_delivery_attempts_f1272a17",
                table: "notification_delivery_attempts",
                column: "notification_message_id",
                principalTable: "notification_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_event_types_54cfe12d",
                table: "notification_event_types",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_event_types_7c7b15b0",
                table: "notification_event_types",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_event_types_bf0de1fd",
                table: "notification_event_types",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_event_types_f5c84fbd",
                table: "notification_event_types",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_event_types_fed61083",
                table: "notification_event_types",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_events_3b90157b",
                table: "notification_events",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_events_58c4d7af",
                table: "notification_events",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_events_96dab6c4",
                table: "notification_events",
                column: "notification_event_type_id",
                principalTable: "notification_event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_events_a96480c4",
                table: "notification_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_inbox_items_34305b9a",
                table: "notification_inbox_items",
                column: "notification_message_id",
                principalTable: "notification_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_inbox_items_36b291a8",
                table: "notification_inbox_items",
                column: "platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_inbox_items_4798709c",
                table: "notification_inbox_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_inbox_items_bfd2c259",
                table: "notification_inbox_items",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_inbox_items_f47d7f24",
                table: "notification_inbox_items",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_06db3c2c",
                table: "notification_messages",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_46b00891",
                table: "notification_messages",
                column: "platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_8e53e11f",
                table: "notification_messages",
                column: "notification_template_version_id",
                principalTable: "notification_template_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_9fc42c44",
                table: "notification_messages",
                column: "notification_channel_id",
                principalTable: "notification_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_bf6a679d",
                table: "notification_messages",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_c47ed18f",
                table: "notification_messages",
                column: "notification_event_id",
                principalTable: "notification_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_messages_d2afb07b",
                table: "notification_messages",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_896fd101",
                table: "notification_preferences",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_aa2a71c0",
                table: "notification_preferences",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_ac287704",
                table: "notification_preferences",
                column: "notification_event_type_id",
                principalTable: "notification_event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_b7806006",
                table: "notification_preferences",
                column: "platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_preferences_d326845c",
                table: "notification_preferences",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_77d5c9dd",
                table: "notification_read_receipts",
                column: "tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_a2927305",
                table: "notification_read_receipts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_aeb35063",
                table: "notification_read_receipts",
                column: "platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_b4b32f67",
                table: "notification_read_receipts",
                column: "notification_message_id",
                principalTable: "notification_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_cdc7161e",
                table: "notification_read_receipts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_read_receipts_faddad41",
                table: "notification_read_receipts",
                column: "notification_inbox_item_id",
                principalTable: "notification_inbox_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_template_versions_055f2a22",
                table: "notification_template_versions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_template_versions_0d2d30cd",
                table: "notification_template_versions",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_template_versions_3d56cee8",
                table: "notification_template_versions",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_template_versions_80211f79",
                table: "notification_template_versions",
                column: "notification_template_id",
                principalTable: "notification_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_19cafb25",
                table: "notification_templates",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_290e3818",
                table: "notification_templates",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_4c2311ef",
                table: "notification_templates",
                column: "archived_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_634a6b74",
                table: "notification_templates",
                column: "notification_event_type_id",
                principalTable: "notification_event_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_6e002c5f",
                table: "notification_templates",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_8bf9119f",
                table: "notification_templates",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_a904d57e",
                table: "notification_templates",
                column: "archived_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notification_templates_d4c90c26",
                table: "notification_templates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_4165eaf4",
                table: "offline_clients",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_501ce399",
                table: "offline_clients",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_7adf2ed4",
                table: "offline_clients",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_c5114e92",
                table: "offline_clients",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_clients_d9af8e18",
                table: "offline_clients",
                column: "pos_device_id",
                principalTable: "pos_devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_id_mappings_6b4b0a22",
                table: "offline_id_mappings",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_id_mappings_8d80eb03",
                table: "offline_id_mappings",
                column: "created_from_sync_item_id",
                principalTable: "sync_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_id_mappings_e181ae14",
                table: "offline_id_mappings",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_number_blocks_1346232d",
                table: "offline_number_blocks",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_number_blocks_a2188000",
                table: "offline_number_blocks",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_offline_number_blocks_fae23a2a",
                table: "offline_number_blocks",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_methods_61d22a01",
                table: "payment_methods",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_methods_8c525868",
                table: "payment_methods",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_methods_b8bcb1a7",
                table: "payment_methods",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_order_events_7f9983ca",
                table: "pickup_order_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_order_events_9194aa75",
                table: "pickup_order_events",
                column: "event_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_order_events_d220b84e",
                table: "pickup_order_events",
                column: "pickup_order_id",
                principalTable: "pickup_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_4c9803c3",
                table: "pickup_orders",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_551f3774",
                table: "pickup_orders",
                column: "verified_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_7fd43ab8",
                table: "pickup_orders",
                column: "fulfillment_order_id",
                principalTable: "fulfillment_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_9460bb7b",
                table: "pickup_orders",
                column: "pickup_slot_reservation_id",
                principalTable: "pickup_slot_reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_orders_e45489f4",
                table: "pickup_orders",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slot_reservations_04fa082b",
                table: "pickup_slot_reservations",
                column: "pickup_slot_id",
                principalTable: "pickup_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slot_reservations_0ceb7dfa",
                table: "pickup_slot_reservations",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slot_reservations_2fb817a0",
                table: "pickup_slot_reservations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slot_reservations_7b279b41",
                table: "pickup_slot_reservations",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slots_5580f869",
                table: "pickup_slots",
                column: "fulfillment_method_outlet_id",
                principalTable: "fulfillment_method_outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pickup_slots_8275d8d5",
                table: "pickup_slots",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_credentials_308650a2",
                table: "platform_integration_credentials",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_credentials_fd56824e",
                table: "platform_integration_credentials",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_request_logs_210d3375",
                table: "platform_integration_request_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_request_logs_c3a27762",
                table: "platform_integration_request_logs",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_request_logs_df4e0286",
                table: "platform_integration_request_logs",
                column: "integration_provider_id",
                principalTable: "integration_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_webhook_events_b9ac16f8",
                table: "platform_integration_webhook_events",
                column: "platform_integration_id",
                principalTable: "platform_integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_webhook_events_dec9d3ba",
                table: "platform_integration_webhook_events",
                column: "integration_provider_id",
                principalTable: "integration_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integration_webhook_events_fe8d195a",
                table: "platform_integration_webhook_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integrations_132de113",
                table: "platform_integrations",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integrations_3bea47ad",
                table: "platform_integrations",
                column: "integration_provider_id",
                principalTable: "integration_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_integrations_ea0ba30a",
                table: "platform_integrations",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_inspections_80f12ba2",
                table: "return_inspections",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_inspections_8666fede",
                table: "return_inspections",
                column: "inventory_location_id",
                principalTable: "inventory_locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_inspections_9faf0f8c",
                table: "return_inspections",
                column: "inspected_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_inspections_a1781c39",
                table: "return_inspections",
                column: "sales_return_line_id",
                principalTable: "sales_return_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_reasons_83eeafe4",
                table: "return_reasons",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_events_23d6ab17",
                table: "sales_exchange_events",
                column: "sales_exchange_id",
                principalTable: "sales_exchanges",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_events_9a6cae34",
                table: "sales_exchange_events",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_events_f22e3e9c",
                table: "sales_exchange_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_2fc89440",
                table: "sales_exchange_lines",
                column: "replacement_product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_6a490826",
                table: "sales_exchange_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_95402dad",
                table: "sales_exchange_lines",
                column: "replacement_sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_aacc4f1a",
                table: "sales_exchange_lines",
                column: "sales_exchange_id",
                principalTable: "sales_exchanges",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_b203b3aa",
                table: "sales_exchange_lines",
                column: "sales_return_line_id",
                principalTable: "sales_return_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchange_lines_c7ddff73",
                table: "sales_exchange_lines",
                column: "replacement_product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_1735e151",
                table: "sales_exchanges",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_42236def",
                table: "sales_exchanges",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_86babc7e",
                table: "sales_exchanges",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_c51ae4c1",
                table: "sales_exchanges",
                column: "replacement_sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_c5dff21b",
                table: "sales_exchanges",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_exchanges_f66bba6d",
                table: "sales_exchanges",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_events_7033c252",
                table: "sales_payment_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_events_832fd9c0",
                table: "sales_payment_events",
                column: "sales_payment_id",
                principalTable: "sales_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_events_ea76aac1",
                table: "sales_payment_events",
                column: "event_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_transactions_31a79680",
                table: "sales_payment_transactions",
                column: "processed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_transactions_b80a12d3",
                table: "sales_payment_transactions",
                column: "sales_payment_id",
                principalTable: "sales_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_transactions_d1461364",
                table: "sales_payment_transactions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payment_transactions_d36a2128",
                table: "sales_payment_transactions",
                column: "parent_transaction_id",
                principalTable: "sales_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_135a8ffe",
                table: "sales_payments",
                column: "payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_3f9e9e72",
                table: "sales_payments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_4d0fbeda",
                table: "sales_payments",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_5f190cb1",
                table: "sales_payments",
                column: "till_session_id",
                principalTable: "till_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_860f8895",
                table: "sales_payments",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_8e5500f7",
                table: "sales_payments",
                column: "till_id",
                principalTable: "tills",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_d4416ae5",
                table: "sales_payments",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_f65f5c53",
                table: "sales_payments",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_payments_fc091449",
                table: "sales_payments",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_lines_66acc628",
                table: "sales_refund_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_lines_9ae29b0c",
                table: "sales_refund_lines",
                column: "sales_refund_id",
                principalTable: "sales_refunds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_lines_b8e16b29",
                table: "sales_refund_lines",
                column: "sales_return_line_id",
                principalTable: "sales_return_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_71431eb5",
                table: "sales_refund_payment_allocations",
                column: "refund_payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_8f5df16a",
                table: "sales_refund_payment_allocations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_a46d161a",
                table: "sales_refund_payment_allocations",
                column: "refund_transaction_id",
                principalTable: "sales_payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_b7012693",
                table: "sales_refund_payment_allocations",
                column: "original_sales_payment_id",
                principalTable: "sales_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refund_payment_allocations_d8eb2e60",
                table: "sales_refund_payment_allocations",
                column: "sales_refund_id",
                principalTable: "sales_refunds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_1d9c0edc",
                table: "sales_refunds",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_29c32df7",
                table: "sales_refunds",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_3786682a",
                table: "sales_refunds",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_37ee683d",
                table: "sales_refunds",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_5c636a91",
                table: "sales_refunds",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_bcb663c6",
                table: "sales_refunds",
                column: "approved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_cc7855ab",
                table: "sales_refunds",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_refunds_dded10a1",
                table: "sales_refunds",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_events_41205a67",
                table: "sales_return_events",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_events_6cb994e3",
                table: "sales_return_events",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_events_c8e9d9b9",
                table: "sales_return_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_1e6282f1",
                table: "sales_return_lines",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_4427f485",
                table: "sales_return_lines",
                column: "return_reason_id",
                principalTable: "return_reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_5de350c6",
                table: "sales_return_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_6cf63e7f",
                table: "sales_return_lines",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_06232f9d",
                table: "sales_returns",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_1549e8c2",
                table: "sales_returns",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_341b4dbd",
                table: "sales_returns",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_43c7eee4",
                table: "sales_returns",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_8308f645",
                table: "sales_returns",
                column: "return_reason_id",
                principalTable: "return_reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_8bbbda2e",
                table: "sales_returns",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_8e3771a4",
                table: "sales_returns",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_returns_f8fcc58d",
                table: "sales_returns",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_batches_28c7db3d",
                table: "sync_batches",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_batches_e699358e",
                table: "sync_batches",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_6b3e3426",
                table: "sync_conflicts",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_8f32d9fc",
                table: "sync_conflicts",
                column: "sync_item_id",
                principalTable: "sync_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_c8d8863b",
                table: "sync_conflicts",
                column: "resolved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_ca8c9281",
                table: "sync_conflicts",
                column: "sync_batch_id",
                principalTable: "sync_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_conflicts_cbd64441",
                table: "sync_conflicts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_items_23643150",
                table: "sync_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_items_c57862d6",
                table: "sync_items",
                column: "offline_client_id",
                principalTable: "offline_clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sync_items_e950bc88",
                table: "sync_items",
                column: "sync_batch_id",
                principalTable: "sync_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_device_sync_states_a0868895",
                table: "device_sync_states");

            migrationBuilder.DropForeignKey(
                name: "fk_device_sync_states_fe00dcdd",
                table: "device_sync_states");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_6480bfac",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_821f4b29",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_bd06d410",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_e627f2fa",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_method_outlets_f359be5f",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_methods_44d027b0",
                table: "fulfillment_methods");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_methods_6a6a1daa",
                table: "fulfillment_methods");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_methods_87b8bdca",
                table: "fulfillment_methods");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_events_8fff23fa",
                table: "fulfillment_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_events_ad8a6f3d",
                table: "fulfillment_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_events_beeeae6b",
                table: "fulfillment_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_5ac519a0",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_9a8cfc05",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_aff467be",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_bcaca9ef",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_ec4817ec",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_f29543cf",
                table: "fulfillment_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_0197fb4f",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_076928b6",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_8d547aff",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_93e151fe",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_b268c630",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_baf8addd",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_d02b8cf5",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_orders_dc59b3c6",
                table: "fulfillment_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_068b9801",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_0fe25ca8",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_6582c928",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_6c57bfdf",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_bd6812f0",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_c29ac65e",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_channels_e57275e8",
                table: "notification_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_delivery_attempts_117f17e1",
                table: "notification_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_delivery_attempts_f1272a17",
                table: "notification_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_event_types_54cfe12d",
                table: "notification_event_types");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_event_types_7c7b15b0",
                table: "notification_event_types");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_event_types_bf0de1fd",
                table: "notification_event_types");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_event_types_f5c84fbd",
                table: "notification_event_types");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_event_types_fed61083",
                table: "notification_event_types");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_events_3b90157b",
                table: "notification_events");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_events_58c4d7af",
                table: "notification_events");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_events_96dab6c4",
                table: "notification_events");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_events_a96480c4",
                table: "notification_events");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_inbox_items_34305b9a",
                table: "notification_inbox_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_inbox_items_36b291a8",
                table: "notification_inbox_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_inbox_items_4798709c",
                table: "notification_inbox_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_inbox_items_bfd2c259",
                table: "notification_inbox_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_inbox_items_f47d7f24",
                table: "notification_inbox_items");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_06db3c2c",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_46b00891",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_8e53e11f",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_9fc42c44",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_bf6a679d",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_c47ed18f",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_messages_d2afb07b",
                table: "notification_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_896fd101",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_aa2a71c0",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_ac287704",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_b7806006",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_preferences_d326845c",
                table: "notification_preferences");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_77d5c9dd",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_a2927305",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_aeb35063",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_b4b32f67",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_cdc7161e",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_read_receipts_faddad41",
                table: "notification_read_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_template_versions_055f2a22",
                table: "notification_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_template_versions_0d2d30cd",
                table: "notification_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_template_versions_3d56cee8",
                table: "notification_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_template_versions_80211f79",
                table: "notification_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_19cafb25",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_290e3818",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_4c2311ef",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_634a6b74",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_6e002c5f",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_8bf9119f",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_a904d57e",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_notification_templates_d4c90c26",
                table: "notification_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_4165eaf4",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_501ce399",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_7adf2ed4",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_c5114e92",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_clients_d9af8e18",
                table: "offline_clients");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_id_mappings_6b4b0a22",
                table: "offline_id_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_id_mappings_8d80eb03",
                table: "offline_id_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_id_mappings_e181ae14",
                table: "offline_id_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_number_blocks_1346232d",
                table: "offline_number_blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_number_blocks_a2188000",
                table: "offline_number_blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_offline_number_blocks_fae23a2a",
                table: "offline_number_blocks");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_methods_61d22a01",
                table: "payment_methods");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_methods_8c525868",
                table: "payment_methods");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_methods_b8bcb1a7",
                table: "payment_methods");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_order_events_7f9983ca",
                table: "pickup_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_order_events_9194aa75",
                table: "pickup_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_order_events_d220b84e",
                table: "pickup_order_events");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_4c9803c3",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_551f3774",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_7fd43ab8",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_9460bb7b",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_orders_e45489f4",
                table: "pickup_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slot_reservations_04fa082b",
                table: "pickup_slot_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slot_reservations_0ceb7dfa",
                table: "pickup_slot_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slot_reservations_2fb817a0",
                table: "pickup_slot_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slot_reservations_7b279b41",
                table: "pickup_slot_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slots_5580f869",
                table: "pickup_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_pickup_slots_8275d8d5",
                table: "pickup_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_credentials_308650a2",
                table: "platform_integration_credentials");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_credentials_fd56824e",
                table: "platform_integration_credentials");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_request_logs_210d3375",
                table: "platform_integration_request_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_request_logs_c3a27762",
                table: "platform_integration_request_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_request_logs_df4e0286",
                table: "platform_integration_request_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_webhook_events_b9ac16f8",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_webhook_events_dec9d3ba",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integration_webhook_events_fe8d195a",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integrations_132de113",
                table: "platform_integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integrations_3bea47ad",
                table: "platform_integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_integrations_ea0ba30a",
                table: "platform_integrations");

            migrationBuilder.DropForeignKey(
                name: "fk_return_inspections_80f12ba2",
                table: "return_inspections");

            migrationBuilder.DropForeignKey(
                name: "fk_return_inspections_8666fede",
                table: "return_inspections");

            migrationBuilder.DropForeignKey(
                name: "fk_return_inspections_9faf0f8c",
                table: "return_inspections");

            migrationBuilder.DropForeignKey(
                name: "fk_return_inspections_a1781c39",
                table: "return_inspections");

            migrationBuilder.DropForeignKey(
                name: "fk_return_reasons_83eeafe4",
                table: "return_reasons");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_events_23d6ab17",
                table: "sales_exchange_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_events_9a6cae34",
                table: "sales_exchange_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_events_f22e3e9c",
                table: "sales_exchange_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_2fc89440",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_6a490826",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_95402dad",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_aacc4f1a",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_b203b3aa",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchange_lines_c7ddff73",
                table: "sales_exchange_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_1735e151",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_42236def",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_86babc7e",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_c51ae4c1",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_c5dff21b",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_exchanges_f66bba6d",
                table: "sales_exchanges");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_events_7033c252",
                table: "sales_payment_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_events_832fd9c0",
                table: "sales_payment_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_events_ea76aac1",
                table: "sales_payment_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_31a79680",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_b80a12d3",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_d1461364",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payment_transactions_d36a2128",
                table: "sales_payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_135a8ffe",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_3f9e9e72",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_4d0fbeda",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_5f190cb1",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_860f8895",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_8e5500f7",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_d4416ae5",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_f65f5c53",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_payments_fc091449",
                table: "sales_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_lines_66acc628",
                table: "sales_refund_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_lines_9ae29b0c",
                table: "sales_refund_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_lines_b8e16b29",
                table: "sales_refund_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_71431eb5",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_8f5df16a",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_a46d161a",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_b7012693",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refund_payment_allocations_d8eb2e60",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_1d9c0edc",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_29c32df7",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_3786682a",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_37ee683d",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_5c636a91",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_bcb663c6",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_cc7855ab",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_refunds_dded10a1",
                table: "sales_refunds");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_events_41205a67",
                table: "sales_return_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_events_6cb994e3",
                table: "sales_return_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_events_c8e9d9b9",
                table: "sales_return_events");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_1e6282f1",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_4427f485",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_5de350c6",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_6cf63e7f",
                table: "sales_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_06232f9d",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_1549e8c2",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_341b4dbd",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_43c7eee4",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_8308f645",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_8bbbda2e",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_8e3771a4",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_returns_f8fcc58d",
                table: "sales_returns");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_batches_28c7db3d",
                table: "sync_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_batches_e699358e",
                table: "sync_batches");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_6b3e3426",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_8f32d9fc",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_c8d8863b",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_ca8c9281",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_conflicts_cbd64441",
                table: "sync_conflicts");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_items_23643150",
                table: "sync_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_items_c57862d6",
                table: "sync_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sync_items_e950bc88",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "IX_sync_items_offline_client_id",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "IX_sync_items_sync_batch_id",
                table: "sync_items");

            migrationBuilder.DropIndex(
                name: "ux_sync_items_ae7f345a",
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

            migrationBuilder.DropIndex(
                name: "IX_sync_conflicts_tenant_id",
                table: "sync_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_sync_batches_offline_client_id",
                table: "sync_batches");

            migrationBuilder.DropIndex(
                name: "ux_sync_batches_a99603d8",
                table: "sync_batches");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_created_by_tenant_user_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_customer_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_document_number_sequence_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_outlet_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_return_reason_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_sales_order_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "IX_sales_returns_updated_by_tenant_user_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ux_sales_returns_35ae5e87",
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

            migrationBuilder.DropIndex(
                name: "IX_sales_return_lines_tenant_id",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_events_created_by_tenant_user_id",
                table: "sales_return_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_events_sales_return_id",
                table: "sales_return_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_return_events_tenant_id",
                table: "sales_return_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_approved_by_tenant_user_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_created_by_tenant_user_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_currency_code",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_document_number_sequence_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_sales_order_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_sales_return_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refunds_updated_by_tenant_user_id",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "ux_sales_refunds_bf2fab40",
                table: "sales_refunds");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_original_sales_payment_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_refund_payment_method_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_refund_transaction_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_sales_refund_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_payment_allocations_tenant_id",
                table: "sales_refund_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_lines_sales_refund_id",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_lines_sales_return_line_id",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_refund_lines_tenant_id",
                table: "sales_refund_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_created_by_tenant_user_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_currency_code",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_document_number_sequence_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_payment_method_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_sales_order_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_till_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_till_session_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payments_updated_by_tenant_user_id",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "ux_sales_payments_3aae300c",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "ux_sales_payments_805b1537",
                table: "sales_payments");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_transactions_parent_transaction_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_transactions_processed_by_tenant_user_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_transactions_sales_payment_id",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_transactions_5562416e",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_transactions_e759526b",
                table: "sales_payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_events_event_by_tenant_user_id",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_payment_events_sales_payment_id",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "ux_sales_payment_events_f38768c9",
                table: "sales_payment_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_created_by_tenant_user_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_document_number_sequence_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_replacement_sales_order_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_sales_return_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchanges_updated_by_tenant_user_id",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "ux_sales_exchanges_3413dbba",
                table: "sales_exchanges");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_replacement_product_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_replacement_product_variant_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_replacement_sales_order_line_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_sales_exchange_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_sales_return_line_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_lines_tenant_id",
                table: "sales_exchange_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_events_created_by_tenant_user_id",
                table: "sales_exchange_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_events_sales_exchange_id",
                table: "sales_exchange_events");

            migrationBuilder.DropIndex(
                name: "IX_sales_exchange_events_tenant_id",
                table: "sales_exchange_events");

            migrationBuilder.DropIndex(
                name: "ux_return_reasons_978380c7",
                table: "return_reasons");

            migrationBuilder.DropIndex(
                name: "IX_return_inspections_inspected_by_tenant_user_id",
                table: "return_inspections");

            migrationBuilder.DropIndex(
                name: "IX_return_inspections_inventory_location_id",
                table: "return_inspections");

            migrationBuilder.DropIndex(
                name: "IX_return_inspections_sales_return_line_id",
                table: "return_inspections");

            migrationBuilder.DropIndex(
                name: "IX_return_inspections_tenant_id",
                table: "return_inspections");

            migrationBuilder.DropIndex(
                name: "IX_platform_integrations_created_by_platform_user_id",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "IX_platform_integrations_currency_code",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "IX_platform_integrations_integration_provider_id",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "ux_platform_integrations_04f0d1d4",
                table: "platform_integrations");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_webhook_events_platform_integration_id",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_webhook_events_tenant_id",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "ux_platform_integration_webhook_events_900a6647",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "ux_platform_integration_webhook_events_be2c7f88",
                table: "platform_integration_webhook_events");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_request_logs_integration_provider_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_request_logs_platform_integration_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_request_logs_tenant_id",
                table: "platform_integration_request_logs");

            migrationBuilder.DropIndex(
                name: "IX_platform_integration_credentials_created_by_platform_user_id",
                table: "platform_integration_credentials");

            migrationBuilder.DropIndex(
                name: "ux_platform_integration_credentials_f64f2b19",
                table: "platform_integration_credentials");

            migrationBuilder.DropIndex(
                name: "IX_pickup_slots_fulfillment_method_outlet_id",
                table: "pickup_slots");

            migrationBuilder.DropIndex(
                name: "ux_pickup_slots_d08294ab",
                table: "pickup_slots");

            migrationBuilder.DropIndex(
                name: "IX_pickup_slot_reservations_checkout_session_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropIndex(
                name: "IX_pickup_slot_reservations_pickup_slot_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropIndex(
                name: "IX_pickup_slot_reservations_sales_order_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropIndex(
                name: "IX_pickup_slot_reservations_tenant_id",
                table: "pickup_slot_reservations");

            migrationBuilder.DropIndex(
                name: "IX_pickup_orders_document_number_sequence_id",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "IX_pickup_orders_fulfillment_order_id",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "IX_pickup_orders_pickup_slot_reservation_id",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "IX_pickup_orders_verified_by_tenant_user_id",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "ux_pickup_orders_917d8d64",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "ux_pickup_orders_9c361648",
                table: "pickup_orders");

            migrationBuilder.DropIndex(
                name: "IX_pickup_order_events_event_by_tenant_user_id",
                table: "pickup_order_events");

            migrationBuilder.DropIndex(
                name: "IX_pickup_order_events_pickup_order_id",
                table: "pickup_order_events");

            migrationBuilder.DropIndex(
                name: "IX_pickup_order_events_tenant_id",
                table: "pickup_order_events");

            migrationBuilder.DropIndex(
                name: "IX_payment_methods_created_by_tenant_user_id",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "IX_payment_methods_updated_by_tenant_user_id",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "ux_payment_methods_d1d0cc7d",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "IX_offline_number_blocks_document_number_sequence_id",
                table: "offline_number_blocks");

            migrationBuilder.DropIndex(
                name: "IX_offline_number_blocks_offline_client_id",
                table: "offline_number_blocks");

            migrationBuilder.DropIndex(
                name: "IX_offline_number_blocks_tenant_id",
                table: "offline_number_blocks");

            migrationBuilder.DropIndex(
                name: "IX_offline_id_mappings_created_from_sync_item_id",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "IX_offline_id_mappings_offline_client_id",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "ux_offline_id_mappings_34294dee",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "ux_offline_id_mappings_e7802dc9",
                table: "offline_id_mappings");

            migrationBuilder.DropIndex(
                name: "IX_offline_clients_created_by_tenant_user_id",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "IX_offline_clients_outlet_id",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "IX_offline_clients_pos_device_id",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "IX_offline_clients_updated_by_tenant_user_id",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "ux_offline_clients_60137369",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "ux_offline_clients_86310fa1",
                table: "offline_clients");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_archived_by_platform_user_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_archived_by_tenant_user_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_created_by_platform_user_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_created_by_tenant_user_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_notification_event_type_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_updated_by_platform_user_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_templates_updated_by_tenant_user_id",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "ux_notification_templates_14448ba8",
                table: "notification_templates");

            migrationBuilder.DropIndex(
                name: "IX_notification_template_versions_created_by_platform_user_id",
                table: "notification_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_notification_template_versions_created_by_tenant_user_id",
                table: "notification_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_notification_template_versions_tenant_id",
                table: "notification_template_versions");

            migrationBuilder.DropIndex(
                name: "ux_notification_template_versions_d19e2b03",
                table: "notification_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_customer_id",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_notification_inbox_item_id",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_notification_message_id",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_platform_user_id",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_tenant_id",
                table: "notification_read_receipts");

            migrationBuilder.DropIndex(
                name: "IX_notification_read_receipts_tenant_user_id",
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
                name: "IX_notification_preferences_tenant_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_preferences_tenant_user_id",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "IX_notification_messages_customer_id",
                table: "notification_messages");

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
                name: "IX_notification_messages_platform_user_id",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "IX_notification_messages_tenant_user_id",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "ux_notification_messages_02c0854c",
                table: "notification_messages");

            migrationBuilder.DropIndex(
                name: "IX_notification_inbox_items_customer_id",
                table: "notification_inbox_items");

            migrationBuilder.DropIndex(
                name: "IX_notification_inbox_items_platform_user_id",
                table: "notification_inbox_items");

            migrationBuilder.DropIndex(
                name: "IX_notification_inbox_items_tenant_id",
                table: "notification_inbox_items");

            migrationBuilder.DropIndex(
                name: "IX_notification_inbox_items_tenant_user_id",
                table: "notification_inbox_items");

            migrationBuilder.DropIndex(
                name: "ux_notification_inbox_items_5e98743f",
                table: "notification_inbox_items");

            migrationBuilder.DropIndex(
                name: "IX_notification_events_created_by_platform_user_id",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "IX_notification_events_created_by_tenant_user_id",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "IX_notification_events_notification_event_type_id",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "ux_notification_events_05be9a99",
                table: "notification_events");

            migrationBuilder.DropIndex(
                name: "IX_notification_event_types_created_by_platform_user_id",
                table: "notification_event_types");

            migrationBuilder.DropIndex(
                name: "IX_notification_event_types_created_by_tenant_user_id",
                table: "notification_event_types");

            migrationBuilder.DropIndex(
                name: "IX_notification_event_types_updated_by_platform_user_id",
                table: "notification_event_types");

            migrationBuilder.DropIndex(
                name: "IX_notification_event_types_updated_by_tenant_user_id",
                table: "notification_event_types");

            migrationBuilder.DropIndex(
                name: "ux_notification_event_types_ff3ad4c7",
                table: "notification_event_types");

            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_attempts_tenant_id",
                table: "notification_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ux_notification_delivery_attempts_59643ebe",
                table: "notification_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_archived_by_platform_user_id",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_archived_by_tenant_user_id",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_created_by_platform_user_id",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_created_by_tenant_user_id",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_updated_by_platform_user_id",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "IX_notification_channels_updated_by_tenant_user_id",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "ux_notification_channels_5708f727",
                table: "notification_channels");

            migrationBuilder.DropIndex(
                name: "ux_integration_providers_b845f32a",
                table: "integration_providers");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_assigned_to_tenant_user_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_created_by_tenant_user_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_document_number_sequence_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_fulfillment_method_outlet_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_sales_order_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_source_inventory_location_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_orders_updated_by_tenant_user_id",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "ux_fulfillment_orders_e767fb12",
                table: "fulfillment_orders");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_fulfillment_order_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_packed_by_tenant_user_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_picked_by_tenant_user_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_component_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_sales_order_line_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_tenant_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_events_event_by_tenant_user_id",
                table: "fulfillment_order_events");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_events_fulfillment_order_id",
                table: "fulfillment_order_events");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_events_tenant_id",
                table: "fulfillment_order_events");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_methods_created_by_tenant_user_id",
                table: "fulfillment_methods");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_methods_updated_by_tenant_user_id",
                table: "fulfillment_methods");

            migrationBuilder.DropIndex(
                name: "ux_fulfillment_methods_4ec69d59",
                table: "fulfillment_methods");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_method_outlets_created_by_tenant_user_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_method_outlets_fulfillment_method_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_method_outlets_outlet_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_method_outlets_updated_by_tenant_user_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "ux_fulfillment_method_outlets_cf79fc78",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "IX_device_sync_states_offline_client_id",
                table: "device_sync_states");

            migrationBuilder.DropIndex(
                name: "ux_device_sync_states_8060ebb9",
                table: "device_sync_states");
        }
    }
}
