using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropLineNumberColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_checkout_events_checkout_session_id_checkout_sessions",
                table: "checkout_events");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_addresses_checkout_session_id_checkout_sessions",
                table: "checkout_session_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_line_options_checkout_session_line_id_checkout_session_lines",
                table: "checkout_session_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_lines_checkout_session_id_checkout_sessions",
                table: "checkout_session_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_shopping_cart_id_shopping_carts",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_item_options_shopping_cart_item_id_shopping_cart_items",
                table: "shopping_cart_item_options");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_items_shopping_cart_id_shopping_carts",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_cart_status",
                table: "shopping_carts");

            migrationBuilder.DropIndex(
                name: "uq_shopping_cart_items_shopping_cart_id_line_number",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_item_options_quantity",
                table: "shopping_cart_item_options");

            migrationBuilder.DropIndex(
                name: "uq_checkout_session_lines_checkout_session_id_line_number",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_line_options_quantity",
                table: "checkout_session_line_options");

            migrationBuilder.DropIndex(
                name: "uq_checkout_events_checkout_session_id_sequence_number",
                table: "checkout_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_events_sequence_number",
                table: "checkout_events");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "sequence_number",
                table: "checkout_events");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "checkout_sessions",
                newName: "checkout_status");

            migrationBuilder.RenameColumn(
                name: "shopping_cart_id",
                table: "checkout_sessions",
                newName: "cart_id");

            migrationBuilder.RenameIndex(
                name: "IX_checkout_sessions_shopping_cart_id",
                table: "checkout_sessions",
                newName: "IX_checkout_sessions_cart_id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "checkout_events",
                newName: "event_at");

            migrationBuilder.AddColumn<string>(
                name: "anonymous_session_id",
                table: "shopping_carts",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "charge_amount",
                table: "shopping_carts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "converted_checkout_session_id",
                table: "shopping_carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "converted_order_id",
                table: "shopping_carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "shopping_carts",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "shopping_carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "shopping_carts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "shopping_carts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sales_channel",
                table: "shopping_carts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "sales_channel_id",
                table: "shopping_carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "shopping_carts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "shopping_carts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_amount",
                table: "shopping_carts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_amount",
                table: "shopping_cart_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "shopping_cart_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "line_subtotal_amount",
                table: "shopping_cart_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_tax_amount",
                table: "shopping_cart_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total_amount",
                table: "shopping_cart_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "shopping_cart_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "product_name_snapshot",
                table: "shopping_cart_items",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_structure",
                table: "shopping_cart_items",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "shopping_cart_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku_snapshot",
                table: "shopping_cart_items",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "shopping_cart_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "shopping_cart_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "shopping_cart_item_options",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "choice_group_id",
                table: "shopping_cart_item_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "choice_group_name_snapshot",
                table: "shopping_cart_item_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "choice_option_id",
                table: "shopping_cart_item_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "choice_option_name_snapshot",
                table: "shopping_cart_item_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_adjustment",
                table: "shopping_cart_item_options",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "shopping_cart_item_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "checkout_sessions",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "checkout_number",
                table: "checkout_sessions",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "anonymous_session_id",
                table: "checkout_sessions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "charge_amount",
                table: "checkout_sessions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "converted_order_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "checkout_sessions",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "checkout_sessions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expired_at",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_method_code",
                table: "checkout_sessions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_reservation_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_email",
                table: "checkout_sessions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_name",
                table: "checkout_sessions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_contact_phone",
                table: "checkout_sessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sales_channel",
                table: "checkout_sessions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "sales_channel_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "selected_outlet_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "selected_pickup_slot_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "checkout_sessions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "checkout_sessions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "checkout_session_id",
                table: "checkout_session_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_amount",
                table: "checkout_session_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "checkout_session_lines",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "line_subtotal_amount",
                table: "checkout_session_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_tax_amount",
                table: "checkout_session_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total_amount",
                table: "checkout_session_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "checkout_session_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "product_name_snapshot",
                table: "checkout_session_lines",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "checkout_session_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sku_snapshot",
                table: "checkout_session_lines",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "checkout_session_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "checkout_session_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "checkout_session_line_options",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "choice_group_id",
                table: "checkout_session_line_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "choice_group_name_snapshot",
                table: "checkout_session_line_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "choice_option_id",
                table: "checkout_session_line_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "choice_option_name_snapshot",
                table: "checkout_session_line_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price_adjustment",
                table: "checkout_session_line_options",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "checkout_session_line_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "checkout_session_id",
                table: "checkout_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "checkout_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_payload_json",
                table: "checkout_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_status",
                table: "checkout_events",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                table: "checkout_events",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "checkout_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_shopping_carts_customer_id",
                table: "shopping_carts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_carts_sales_channel_id",
                table: "shopping_carts",
                column: "sales_channel_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_cart_status",
                table: "shopping_carts",
                sql: "cart_status IN ('ACTIVE', 'ABANDONED', 'CONVERTED', 'EXPIRED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_charge_amount",
                table: "shopping_carts",
                sql: "charge_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_discount_amount",
                table: "shopping_carts",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_subtotal_amount",
                table: "shopping_carts",
                sql: "subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_tax_amount",
                table: "shopping_carts",
                sql: "tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_total_amount",
                table: "shopping_carts",
                sql: "total_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_items_product_id",
                table: "shopping_cart_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_items_product_variant_id",
                table: "shopping_cart_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_items_shopping_cart_id",
                table: "shopping_cart_items",
                column: "shopping_cart_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_items_tenant_id",
                table: "shopping_cart_items",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_line_discount_amount",
                table: "shopping_cart_items",
                sql: "line_discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_line_status",
                table: "shopping_cart_items",
                sql: "line_status IN ('ACTIVE', 'REMOVED', 'UNAVAILABLE', 'PRICE_CHANGED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_line_subtotal_amount",
                table: "shopping_cart_items",
                sql: "line_subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_line_tax_amount",
                table: "shopping_cart_items",
                sql: "line_tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_line_total_amount",
                table: "shopping_cart_items",
                sql: "line_total_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_items_unit_price",
                table: "shopping_cart_items",
                sql: "unit_price >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_item_options_choice_group_id",
                table: "shopping_cart_item_options",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_item_options_choice_option_id",
                table: "shopping_cart_item_options",
                column: "choice_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_item_options_tenant_id",
                table: "shopping_cart_item_options",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_item_options_sort_order",
                table: "shopping_cart_item_options",
                sql: "sort_order IS NULL OR sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_converted_order_id",
                table: "checkout_sessions",
                column: "converted_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_customer_id",
                table: "checkout_sessions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_sales_channel_id",
                table: "checkout_sessions",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_selected_outlet_id",
                table: "checkout_sessions",
                column: "selected_outlet_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_sessions_charge_amount",
                table: "checkout_sessions",
                sql: "charge_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_sessions_checkout_status",
                table: "checkout_sessions",
                sql: "checkout_status IN ('STARTED', 'PENDING', 'COMPLETED', 'EXPIRED', 'CANCELLED', 'FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_sessions_discount_amount",
                table: "checkout_sessions",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_sessions_subtotal_amount",
                table: "checkout_sessions",
                sql: "subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_sessions_tax_amount",
                table: "checkout_sessions",
                sql: "tax_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_lines_checkout_session_id",
                table: "checkout_session_lines",
                column: "checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_lines_product_id",
                table: "checkout_session_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_lines_product_variant_id",
                table: "checkout_session_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_lines_tenant_id",
                table: "checkout_session_lines",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_lines_line_discount_amount",
                table: "checkout_session_lines",
                sql: "line_discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_lines_line_subtotal_amount",
                table: "checkout_session_lines",
                sql: "line_subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_lines_line_tax_amount",
                table: "checkout_session_lines",
                sql: "line_tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_lines_line_total_amount",
                table: "checkout_session_lines",
                sql: "line_total_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_lines_unit_price",
                table: "checkout_session_lines",
                sql: "unit_price >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_line_options_choice_group_id",
                table: "checkout_session_line_options",
                column: "choice_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_line_options_choice_option_id",
                table: "checkout_session_line_options",
                column: "choice_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_line_options_tenant_id",
                table: "checkout_session_line_options",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_line_options_sort_order",
                table: "checkout_session_line_options",
                sql: "sort_order IS NULL OR sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_addresses_address_type",
                table: "checkout_session_addresses",
                sql: "address_type IN ('BILLING', 'SHIPPING', 'PICKUP')");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_events_checkout_session_id",
                table: "checkout_events",
                column: "checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_events_created_by_tenant_user_id",
                table: "checkout_events",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_events_tenant_id",
                table: "checkout_events",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_events_checkout_session_id_checkout_sessions",
                table: "checkout_events",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_events_created_by_tenant_user_id_tenant_users",
                table: "checkout_events",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_events_tenant_id_tenants",
                table: "checkout_events",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_addresses_checkout_session_id_checkout_sessions",
                table: "checkout_session_addresses",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_line_options_checkout_session_line_id_checkout_session_lines",
                table: "checkout_session_line_options",
                column: "checkout_session_line_id",
                principalTable: "checkout_session_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_line_options_choice_group_id_choice_groups",
                table: "checkout_session_line_options",
                column: "choice_group_id",
                principalTable: "choice_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_line_options_choice_option_id_choice_options",
                table: "checkout_session_line_options",
                column: "choice_option_id",
                principalTable: "choice_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_line_options_tenant_id_tenants",
                table: "checkout_session_line_options",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_lines_checkout_session_id_checkout_sessions",
                table: "checkout_session_lines",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_lines_product_id_products",
                table: "checkout_session_lines",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_lines_product_variant_id_product_variants",
                table: "checkout_session_lines",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_lines_tenant_id_tenants",
                table: "checkout_session_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_cart_id_shopping_carts",
                table: "checkout_sessions",
                column: "cart_id",
                principalTable: "shopping_carts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_converted_order_id_sales_orders",
                table: "checkout_sessions",
                column: "converted_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_customer_id_customers",
                table: "checkout_sessions",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_sales_channel_id_sales_channels",
                table: "checkout_sessions",
                column: "sales_channel_id",
                principalTable: "platform_sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_selected_outlet_id_outlets",
                table: "checkout_sessions",
                column: "selected_outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_tenant_id_tenants",
                table: "checkout_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_item_options_choice_group_id_choice_groups",
                table: "shopping_cart_item_options",
                column: "choice_group_id",
                principalTable: "choice_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_item_options_choice_option_id_choice_options",
                table: "shopping_cart_item_options",
                column: "choice_option_id",
                principalTable: "choice_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_item_options_shopping_cart_item_id_shopping_cart_items",
                table: "shopping_cart_item_options",
                column: "shopping_cart_item_id",
                principalTable: "shopping_cart_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_item_options_tenant_id_tenants",
                table: "shopping_cart_item_options",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_items_product_id_products",
                table: "shopping_cart_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_items_product_variant_id_product_variants",
                table: "shopping_cart_items",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_items_shopping_cart_id_shopping_carts",
                table: "shopping_cart_items",
                column: "shopping_cart_id",
                principalTable: "shopping_carts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_items_tenant_id_tenants",
                table: "shopping_cart_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_carts_customer_id_customers",
                table: "shopping_carts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_carts_sales_channel_id_sales_channels",
                table: "shopping_carts",
                column: "sales_channel_id",
                principalTable: "platform_sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_checkout_events_checkout_session_id_checkout_sessions",
                table: "checkout_events");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_events_created_by_tenant_user_id_tenant_users",
                table: "checkout_events");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_events_tenant_id_tenants",
                table: "checkout_events");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_addresses_checkout_session_id_checkout_sessions",
                table: "checkout_session_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_line_options_checkout_session_line_id_checkout_session_lines",
                table: "checkout_session_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_line_options_choice_group_id_choice_groups",
                table: "checkout_session_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_line_options_choice_option_id_choice_options",
                table: "checkout_session_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_line_options_tenant_id_tenants",
                table: "checkout_session_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_lines_checkout_session_id_checkout_sessions",
                table: "checkout_session_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_lines_product_id_products",
                table: "checkout_session_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_lines_product_variant_id_product_variants",
                table: "checkout_session_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_session_lines_tenant_id_tenants",
                table: "checkout_session_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_cart_id_shopping_carts",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_converted_order_id_sales_orders",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_customer_id_customers",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_sales_channel_id_sales_channels",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_selected_outlet_id_outlets",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_checkout_sessions_tenant_id_tenants",
                table: "checkout_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_item_options_choice_group_id_choice_groups",
                table: "shopping_cart_item_options");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_item_options_choice_option_id_choice_options",
                table: "shopping_cart_item_options");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_item_options_shopping_cart_item_id_shopping_cart_items",
                table: "shopping_cart_item_options");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_item_options_tenant_id_tenants",
                table: "shopping_cart_item_options");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_items_product_id_products",
                table: "shopping_cart_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_items_product_variant_id_product_variants",
                table: "shopping_cart_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_items_shopping_cart_id_shopping_carts",
                table: "shopping_cart_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_cart_items_tenant_id_tenants",
                table: "shopping_cart_items");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_carts_customer_id_customers",
                table: "shopping_carts");

            migrationBuilder.DropForeignKey(
                name: "fk_shopping_carts_sales_channel_id_sales_channels",
                table: "shopping_carts");

            migrationBuilder.DropIndex(
                name: "IX_shopping_carts_customer_id",
                table: "shopping_carts");

            migrationBuilder.DropIndex(
                name: "IX_shopping_carts_sales_channel_id",
                table: "shopping_carts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_cart_status",
                table: "shopping_carts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_charge_amount",
                table: "shopping_carts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_discount_amount",
                table: "shopping_carts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_subtotal_amount",
                table: "shopping_carts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_tax_amount",
                table: "shopping_carts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_carts_total_amount",
                table: "shopping_carts");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_items_product_id",
                table: "shopping_cart_items");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_items_product_variant_id",
                table: "shopping_cart_items");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_items_shopping_cart_id",
                table: "shopping_cart_items");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_items_tenant_id",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_line_discount_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_line_status",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_line_subtotal_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_line_tax_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_line_total_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_items_unit_price",
                table: "shopping_cart_items");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_item_options_choice_group_id",
                table: "shopping_cart_item_options");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_item_options_choice_option_id",
                table: "shopping_cart_item_options");

            migrationBuilder.DropIndex(
                name: "IX_shopping_cart_item_options_tenant_id",
                table: "shopping_cart_item_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shopping_cart_item_options_sort_order",
                table: "shopping_cart_item_options");

            migrationBuilder.DropIndex(
                name: "IX_checkout_sessions_converted_order_id",
                table: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_checkout_sessions_customer_id",
                table: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_checkout_sessions_sales_channel_id",
                table: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_checkout_sessions_selected_outlet_id",
                table: "checkout_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_sessions_charge_amount",
                table: "checkout_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_sessions_checkout_status",
                table: "checkout_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_sessions_discount_amount",
                table: "checkout_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_sessions_subtotal_amount",
                table: "checkout_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_sessions_tax_amount",
                table: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_lines_checkout_session_id",
                table: "checkout_session_lines");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_lines_product_id",
                table: "checkout_session_lines");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_lines_product_variant_id",
                table: "checkout_session_lines");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_lines_tenant_id",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_lines_line_discount_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_lines_line_subtotal_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_lines_line_tax_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_lines_line_total_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_lines_unit_price",
                table: "checkout_session_lines");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_line_options_choice_group_id",
                table: "checkout_session_line_options");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_line_options_choice_option_id",
                table: "checkout_session_line_options");

            migrationBuilder.DropIndex(
                name: "IX_checkout_session_line_options_tenant_id",
                table: "checkout_session_line_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_line_options_sort_order",
                table: "checkout_session_line_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_checkout_session_addresses_address_type",
                table: "checkout_session_addresses");

            migrationBuilder.DropIndex(
                name: "IX_checkout_events_checkout_session_id",
                table: "checkout_events");

            migrationBuilder.DropIndex(
                name: "IX_checkout_events_created_by_tenant_user_id",
                table: "checkout_events");

            migrationBuilder.DropIndex(
                name: "IX_checkout_events_tenant_id",
                table: "checkout_events");

            migrationBuilder.DropColumn(
                name: "anonymous_session_id",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "charge_amount",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "converted_checkout_session_id",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "converted_order_id",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "sales_channel",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "sales_channel_id",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "total_amount",
                table: "shopping_carts");

            migrationBuilder.DropColumn(
                name: "line_discount_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "line_subtotal_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "line_tax_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "line_total_amount",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "product_name_snapshot",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "product_structure",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "sku_snapshot",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "shopping_cart_items");

            migrationBuilder.DropColumn(
                name: "choice_group_id",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "choice_group_name_snapshot",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "choice_option_id",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "choice_option_name_snapshot",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "price_adjustment",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "shopping_cart_item_options");

            migrationBuilder.DropColumn(
                name: "anonymous_session_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "charge_amount",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "converted_order_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "expired_at",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "fulfillment_method_code",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "inventory_reservation_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "pickup_contact_email",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "pickup_contact_name",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "pickup_contact_phone",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "sales_channel",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "sales_channel_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "selected_outlet_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "selected_pickup_slot_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "line_discount_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "line_subtotal_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "line_tax_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "line_total_amount",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "product_name_snapshot",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "sku_snapshot",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "checkout_session_lines");

            migrationBuilder.DropColumn(
                name: "choice_group_id",
                table: "checkout_session_line_options");

            migrationBuilder.DropColumn(
                name: "choice_group_name_snapshot",
                table: "checkout_session_line_options");

            migrationBuilder.DropColumn(
                name: "choice_option_id",
                table: "checkout_session_line_options");

            migrationBuilder.DropColumn(
                name: "choice_option_name_snapshot",
                table: "checkout_session_line_options");

            migrationBuilder.DropColumn(
                name: "price_adjustment",
                table: "checkout_session_line_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "checkout_session_line_options");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "checkout_events");

            migrationBuilder.DropColumn(
                name: "event_payload_json",
                table: "checkout_events");

            migrationBuilder.DropColumn(
                name: "event_status",
                table: "checkout_events");

            migrationBuilder.DropColumn(
                name: "event_type",
                table: "checkout_events");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "checkout_events");

            migrationBuilder.RenameColumn(
                name: "checkout_status",
                table: "checkout_sessions",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "cart_id",
                table: "checkout_sessions",
                newName: "shopping_cart_id");

            migrationBuilder.RenameIndex(
                name: "IX_checkout_sessions_cart_id",
                table: "checkout_sessions",
                newName: "IX_checkout_sessions_shopping_cart_id");

            migrationBuilder.RenameColumn(
                name: "event_at",
                table: "checkout_events",
                newName: "updated_at");

            migrationBuilder.AddColumn<string>(
                name: "line_number",
                table: "shopping_cart_items",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "shopping_cart_item_options",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "shopping_cart_item_options",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "checkout_sessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<string>(
                name: "checkout_number",
                table: "checkout_sessions",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<Guid>(
                name: "checkout_session_id",
                table: "checkout_session_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "line_number",
                table: "checkout_session_lines",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "checkout_session_line_options",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "checkout_session_id",
                table: "checkout_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "sequence_number",
                table: "checkout_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_carts_cart_status",
                table: "shopping_carts",
                sql: "cart_status IN ('ACTIVE', 'CHECKED_OUT', 'ABANDONED', 'EXPIRED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "uq_shopping_cart_items_shopping_cart_id_line_number",
                table: "shopping_cart_items",
                columns: new[] { "shopping_cart_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_shopping_cart_item_options_quantity",
                table: "shopping_cart_item_options",
                sql: "quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_checkout_session_lines_checkout_session_id_line_number",
                table: "checkout_session_lines",
                columns: new[] { "checkout_session_id", "line_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_session_line_options_quantity",
                table: "checkout_session_line_options",
                sql: "quantity > 0");

            migrationBuilder.CreateIndex(
                name: "uq_checkout_events_checkout_session_id_sequence_number",
                table: "checkout_events",
                columns: new[] { "checkout_session_id", "sequence_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_checkout_events_sequence_number",
                table: "checkout_events",
                sql: "sequence_number > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_events_checkout_session_id_checkout_sessions",
                table: "checkout_events",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_addresses_checkout_session_id_checkout_sessions",
                table: "checkout_session_addresses",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_line_options_checkout_session_line_id_checkout_session_lines",
                table: "checkout_session_line_options",
                column: "checkout_session_line_id",
                principalTable: "checkout_session_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_session_lines_checkout_session_id_checkout_sessions",
                table: "checkout_session_lines",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_checkout_sessions_shopping_cart_id_shopping_carts",
                table: "checkout_sessions",
                column: "shopping_cart_id",
                principalTable: "shopping_carts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_item_options_shopping_cart_item_id_shopping_cart_items",
                table: "shopping_cart_item_options",
                column: "shopping_cart_item_id",
                principalTable: "shopping_cart_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shopping_cart_items_shopping_cart_id_shopping_carts",
                table: "shopping_cart_items",
                column: "shopping_cart_id",
                principalTable: "shopping_carts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
