using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropTemporaryErdAlignmentDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policies' AND column_name = 'discount_policy_name') THEN
        EXECUTE 'ALTER TABLE public.""discount_policies"" ALTER COLUMN ""discount_policy_name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policies' AND column_name = 'discount_scope') THEN
        EXECUTE 'ALTER TABLE public.""discount_policies"" ALTER COLUMN ""discount_scope"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policies' AND column_name = 'name') THEN
        EXECUTE 'ALTER TABLE public.""discount_policies"" ALTER COLUMN ""name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policies' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""discount_policies"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_channels' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_channels"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_conditions' AND column_name = 'condition_operator') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_conditions"" ALTER COLUMN ""condition_operator"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_conditions' AND column_name = 'condition_type') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_conditions"" ALTER COLUMN ""condition_type"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_conditions' AND column_name = 'condition_value_json') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_conditions"" ALTER COLUMN ""condition_value_json"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_conditions' AND column_name = 'group_operator') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_conditions"" ALTER COLUMN ""group_operator"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_conditions' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_conditions"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_conditions' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_conditions"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_outlets' AND column_name = 'outlet_id') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_outlets"" ALTER COLUMN ""outlet_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_outlets' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_outlets"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_targets' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_targets"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_policy_targets' AND column_name = 'target_mode') THEN
        EXECUTE 'ALTER TABLE public.""discount_policy_targets"" ALTER COLUMN ""target_mode"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_types' AND column_name = 'calculation_method') THEN
        EXECUTE 'ALTER TABLE public.""discount_types"" ALTER COLUMN ""calculation_method"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_types' AND column_name = 'discount_type_name') THEN
        EXECUTE 'ALTER TABLE public.""discount_types"" ALTER COLUMN ""discount_type_name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_types' AND column_name = 'name') THEN
        EXECUTE 'ALTER TABLE public.""discount_types"" ALTER COLUMN ""name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_types' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""discount_types"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'discount_types' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""discount_types"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_applications' AND column_name = 'application_source') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_applications"" ALTER COLUMN ""application_source"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_applications' AND column_name = 'application_status') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_applications"" ALTER COLUMN ""application_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_applications' AND column_name = 'expiry_discount_rule_tier_id') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_applications"" ALTER COLUMN ""expiry_discount_rule_tier_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_applications' AND column_name = 'outlet_id') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_applications"" ALTER COLUMN ""outlet_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_applications' AND column_name = 'product_batch_id') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_applications"" ALTER COLUMN ""product_batch_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_applications' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_applications"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_rule_tiers' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_rule_tiers"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_rule_tiers' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_rule_tiers"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_rules' AND column_name = 'discount_policy_id') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_rules"" ALTER COLUMN ""discount_policy_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_rules' AND column_name = 'name') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_rules"" ALTER COLUMN ""name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_rules' AND column_name = 'rule_name') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_rules"" ALTER COLUMN ""rule_name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'expiry_discount_rules' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""expiry_discount_rules"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_balances' AND column_name = 'product_batch_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_balances"" ALTER COLUMN ""product_batch_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_balances' AND column_name = 'product_variant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_balances"" ALTER COLUMN ""product_variant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_balances' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_balances"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_channel_allocations' AND column_name = 'product_variant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_channel_allocations"" ALTER COLUMN ""product_variant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_channel_allocations' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""inventory_channel_allocations"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_channel_allocations' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_channel_allocations"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_cost_layers' AND column_name = 'inventory_balance_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_cost_layers"" ALTER COLUMN ""inventory_balance_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_cost_layers' AND column_name = 'received_at') THEN
        EXECUTE 'ALTER TABLE public.""inventory_cost_layers"" ALTER COLUMN ""received_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_cost_layers' AND column_name = 'source_stock_movement_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_cost_layers"" ALTER COLUMN ""source_stock_movement_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_cost_layers' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""inventory_cost_layers"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_cost_layers' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_cost_layers"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_locations' AND column_name = 'location_name') THEN
        EXECUTE 'ALTER TABLE public.""inventory_locations"" ALTER COLUMN ""location_name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_locations' AND column_name = 'location_type') THEN
        EXECUTE 'ALTER TABLE public.""inventory_locations"" ALTER COLUMN ""location_type"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_locations' AND column_name = 'name') THEN
        EXECUTE 'ALTER TABLE public.""inventory_locations"" ALTER COLUMN ""name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_locations' AND column_name = 'outlet_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_locations"" ALTER COLUMN ""outlet_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_locations' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""inventory_locations"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reorder_rules' AND column_name = 'product_variant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reorder_rules"" ALTER COLUMN ""product_variant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reorder_rules' AND column_name = 'reorder_method') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reorder_rules"" ALTER COLUMN ""reorder_method"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reorder_rules' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reorder_rules"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reorder_rules' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reorder_rules"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservation_allocations' AND column_name = 'allocated_at') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservation_allocations"" ALTER COLUMN ""allocated_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservation_allocations' AND column_name = 'allocation_status') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservation_allocations"" ALTER COLUMN ""allocation_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservation_allocations' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservation_allocations"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservation_lines' AND column_name = 'line_status') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservation_lines"" ALTER COLUMN ""line_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservation_lines' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservation_lines"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservations' AND column_name = 'fulfillment_outlet_id') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservations"" ALTER COLUMN ""fulfillment_outlet_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservations' AND column_name = 'reservation_source') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservations"" ALTER COLUMN ""reservation_source"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'inventory_reservations' AND column_name = 'reserved_at') THEN
        EXECUTE 'ALTER TABLE public.""inventory_reservations"" ALTER COLUMN ""reserved_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'product_inventory_settings' AND column_name = 'costing_method') THEN
        EXECUTE 'ALTER TABLE public.""product_inventory_settings"" ALTER COLUMN ""costing_method"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'product_inventory_settings' AND column_name = 'inventory_uom_id') THEN
        EXECUTE 'ALTER TABLE public.""product_inventory_settings"" ALTER COLUMN ""inventory_uom_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'product_inventory_settings' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""product_inventory_settings"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'product_inventory_settings' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""product_inventory_settings"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'serial_numbers' AND column_name = 'serial_status') THEN
        EXECUTE 'ALTER TABLE public.""serial_numbers"" ALTER COLUMN ""serial_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_adjustment_lines' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_adjustment_lines"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_adjustment_reasons' AND column_name = 'direction') THEN
        EXECUTE 'ALTER TABLE public.""stock_adjustment_reasons"" ALTER COLUMN ""direction"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_adjustment_reasons' AND column_name = 'name') THEN
        EXECUTE 'ALTER TABLE public.""stock_adjustment_reasons"" ALTER COLUMN ""name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_adjustment_reasons' AND column_name = 'reason_name') THEN
        EXECUTE 'ALTER TABLE public.""stock_adjustment_reasons"" ALTER COLUMN ""reason_name"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_adjustment_reasons' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""stock_adjustment_reasons"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movement_cost_allocations' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_movement_cost_allocations"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movement_cost_allocations' AND column_name = 'updated_at') THEN
        EXECUTE 'ALTER TABLE public.""stock_movement_cost_allocations"" ALTER COLUMN ""updated_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movement_references' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_movement_references"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movement_references' AND column_name = 'updated_at') THEN
        EXECUTE 'ALTER TABLE public.""stock_movement_references"" ALTER COLUMN ""updated_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movement_serials' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_movement_serials"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movement_serials' AND column_name = 'updated_at') THEN
        EXECUTE 'ALTER TABLE public.""stock_movement_serials"" ALTER COLUMN ""updated_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movements' AND column_name = 'inventory_balance_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_movements"" ALTER COLUMN ""inventory_balance_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_movements' AND column_name = 'movement_type') THEN
        EXECUTE 'ALTER TABLE public.""stock_movements"" ALTER COLUMN ""movement_type"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfer_lines' AND column_name = 'line_status') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfer_lines"" ALTER COLUMN ""line_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfer_lines' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfer_lines"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfer_status_history' AND column_name = 'created_at') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfer_status_history"" ALTER COLUMN ""created_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfer_status_history' AND column_name = 'new_status') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfer_status_history"" ALTER COLUMN ""new_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfer_status_history' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfer_status_history"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfers' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfers"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stock_transfers' AND column_name = 'transfer_status') THEN
        EXECUTE 'ALTER TABLE public.""stock_transfers"" ALTER COLUMN ""transfer_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_line_serials' AND column_name = 'count_result') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_line_serials"" ALTER COLUMN ""count_result"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_line_serials' AND column_name = 'scanned_serial_number') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_line_serials"" ALTER COLUMN ""scanned_serial_number"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_line_serials' AND column_name = 'serial_number_id') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_line_serials"" ALTER COLUMN ""serial_number_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_line_serials' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_line_serials"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_lines' AND column_name = 'line_status') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_lines"" ALTER COLUMN ""line_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_lines' AND column_name = 'product_batch_id') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_lines"" ALTER COLUMN ""product_batch_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_lines' AND column_name = 'product_variant_id') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_lines"" ALTER COLUMN ""product_variant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_lines' AND column_name = 'tenant_id') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_lines"" ALTER COLUMN ""tenant_id"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_sessions' AND column_name = 'snapshot_at') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_sessions"" ALTER COLUMN ""snapshot_at"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_sessions' AND column_name = 'status') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_sessions"" ALTER COLUMN ""status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_sessions' AND column_name = 'stocktake_status') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_sessions"" ALTER COLUMN ""stocktake_status"" DROP DEFAULT';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stocktake_sessions' AND column_name = 'stocktake_type') THEN
        EXECUTE 'ALTER TABLE public.""stocktake_sessions"" ALTER COLUMN ""stocktake_type"" DROP DEFAULT';
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}