using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule20OrderWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_sales_order_id_sales_orders",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_sales_order_line_id_sales_order_lines",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_options_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_status_history_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_sales_order_id_sales_orders",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_status_history_sales_order_id_sales_orders",
                table: "sales_order_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_sales_order_id_sales_orders",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_sales_order_line_id_sales_order_lines",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_sales_order_id",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_sales_order_line_id",
                table: "sales_order_taxes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_taxes_tax_rate_percent",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_status_history_sales_order_id_sequence_number",
                table: "sales_order_status_history");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_line_status_history_sales_order_line_id_sequence_number",
                table: "sales_order_line_status_history");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_options_sales_order_line_id",
                table: "sales_order_line_options");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_sales_order_line_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_sales_order_id",
                table: "sales_order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_sales_order_line_id",
                table: "sales_order_discounts");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "sales_orders",
                newName: "order_status");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "paid_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "order_status",
                table: "sales_orders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "balance_due",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "sales_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "charge_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "confirmed_at",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "sales_orders",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_email_snapshot",
                table: "sales_orders",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_name_snapshot",
                table: "sales_orders",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_note",
                table: "sales_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_phone_snapshot",
                table: "sales_orders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_order_reference",
                table: "sales_orders",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_method_code_snapshot",
                table: "sales_orders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fulfillment_method_outlet_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fulfillment_status",
                table: "sales_orders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "internal_note",
                table: "sales_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_type",
                table: "sales_orders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "sales_orders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "placed_at",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "price_list_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "rounding_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_channel_id",
                table: "sales_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "sales_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_session_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_rate_percent",
                table: "sales_order_taxes",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "sales_order_taxes",
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
                table: "sales_order_taxes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "calculation_sequence",
                table: "sales_order_taxes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "is_tax_included",
                table: "sales_order_taxes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "jurisdiction_name_snapshot",
                table: "sales_order_taxes",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_class_code_snapshot",
                table: "sales_order_taxes",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_class_id",
                table: "sales_order_taxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_jurisdiction_id",
                table: "sales_order_taxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_name_snapshot",
                table: "sales_order_taxes",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tax_rate_code_snapshot",
                table: "sales_order_taxes",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_rate_id",
                table: "sales_order_taxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "taxable_amount",
                table: "sales_order_taxes",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_taxes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_order_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_reason",
                table: "sales_order_status_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "changed_at",
                table: "sales_order_status_history",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "changed_by_tenant_user_id",
                table: "sales_order_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "sales_order_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "sales_order_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_type",
                table: "sales_order_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total_amount",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.Sql("ALTER TABLE sales_order_lines ALTER COLUMN line_number TYPE integer USING line_number::integer, ALTER COLUMN line_number SET NOT NULL;");

            migrationBuilder.AddColumn<decimal>(
                name: "cancelled_quantity",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "fulfilled_quantity",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_amount",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_status",
                table: "sales_order_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "line_subtotal_amount",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_tax_amount",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "original_unit_price",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "price_list_item_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "product_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_structure_snapshot",
                table: "sales_order_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_type_snapshot",
                table: "sales_order_lines",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "returned_quantity",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "sku_snapshot",
                table: "sales_order_lines",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "sales_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "uom_code_snapshot",
                table: "sales_order_lines",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "uom_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "uom_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "variant_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_order_line_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "affected_quantity",
                table: "sales_order_line_status_history",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_reason",
                table: "sales_order_line_status_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "changed_at",
                table: "sales_order_line_status_history",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "changed_by_tenant_user_id",
                table: "sales_order_line_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "sales_order_line_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "sales_order_line_status_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_line_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "sales_order_line_options",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_order_line_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "sales_order_line_options",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "choice_group_name_snapshot",
                table: "sales_order_line_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "choice_option_name_snapshot",
                table: "sales_order_line_options",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "product_choice_group_id",
                table: "sales_order_line_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_choice_option_id",
                table: "sales_order_line_options",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_line_options",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "total_price_adjustment",
                table: "sales_order_line_options",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price_adjustment",
                table: "sales_order_line_options",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "sales_order_line_components",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "combo_component_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "combo_definition_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "combo_group_item_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_name_snapshot",
                table: "sales_order_line_components",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "item_product_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "item_sku_snapshot",
                table: "sales_order_line_components",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_source_type",
                table: "sales_order_line_components",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "item_uom_code_snapshot",
                table: "sales_order_line_components",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "item_uom_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "item_uom_name_snapshot",
                table: "sales_order_line_components",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "item_variant_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_variant_name_snapshot",
                table: "sales_order_line_components",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "total_price_adjustment",
                table: "sales_order_line_components",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price_adjustment",
                table: "sales_order_line_components",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "sales_order_discounts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "application_sequence",
                table: "sales_order_discounts",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "applied_at",
                table: "sales_order_discounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "applied_by_tenant_user_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "calculation_method_snapshot",
                table: "sales_order_discounts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discount_code_snapshot",
                table: "sales_order_discounts",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_name_snapshot",
                table: "sales_order_discounts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "discount_policy_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_target_scope",
                table: "sales_order_discounts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "discount_type_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "discount_value",
                table: "sales_order_discounts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "manual_discount_reason",
                table: "sales_order_discounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_channel_id",
                table: "document_number_sequences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "padding_length",
                table: "document_number_sequences",
                type: "integer",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "document_number_sequences",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "document_subtype",
                table: "document_number_sequences",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<long>(
                name: "current_value",
                table: "document_number_sequences",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "document_number_sequences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_generated_at",
                table: "document_number_sequences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_reset_at",
                table: "document_number_sequences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prefix",
                table: "document_number_sequences",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "reset_rule",
                table: "document_number_sequences",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "document_number_sequences",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "document_number_sequences",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "suffix",
                table: "document_number_sequences",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "document_number_sequences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_sales_order_lines_tenant_id_id",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_price_list_items_tenant_id_id",
                table: "price_list_items",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_fulfillment_method_outlets_tenant_id_id",
                table: "fulfillment_method_outlets",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_combo_group_items_tenant_id_id",
                table: "combo_group_items",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_combo_components_tenant_id_id",
                table: "combo_components",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "sales_order_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    charge_scope = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    charge_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    charge_name_snapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    charge_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_taxable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    applied_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_charges", x => x.id);
                    table.CheckConstraint("ck_sales_order_charges_charge_amount", "charge_amount >= 0");
                    table.CheckConstraint("ck_sales_order_charges_charge_scope", "charge_scope IN ('ORDER', 'LINE')");
                    table.CheckConstraint("ck_sales_order_charges_charge_type", "charge_type IN ('SERVICE_FEE', 'PACKAGING_FEE', 'ROUNDING', 'OTHER')");
                    table.ForeignKey(
                        name: "fk_sales_order_charges_applied_by_tenant_user_id_tenant_users",
                        column: x => x.applied_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_charges_sales_order_id_sales_orders",
                        columns: x => new { x.tenant_id, x.sales_order_id },
                        principalTable: "sales_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_charges_sales_order_line_id_sales_order_lines",
                        columns: x => new { x.tenant_id, x.sales_order_line_id },
                        principalTable: "sales_order_lines",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_charges_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_created_by_tenant_user_id",
                table: "sales_orders",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_customer_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_document_number_sequence_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "document_number_sequence_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_fulfillment_method_outlet_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "fulfillment_method_outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_price_list_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "price_list_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_sales_channel_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_till_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_till_session_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "till_session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_updated_by_tenant_user_id",
                table: "sales_orders",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_charge_amount",
                table: "sales_orders",
                sql: "charge_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_discount_amount",
                table: "sales_orders",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_fulfillment_status",
                table: "sales_orders",
                sql: "fulfillment_status IN ('NOT_REQUIRED', 'PENDING', 'READY_FOR_PICKUP', 'PARTIALLY_FULFILLED', 'FULFILLED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_order_status",
                table: "sales_orders",
                sql: "order_status IN ('DRAFT', 'PLACED', 'CONFIRMED', 'COMPLETED', 'CANCELLED', 'VOIDED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_order_type",
                table: "sales_orders",
                sql: "order_type IN ('POS_SALE', 'CLICK_AND_COLLECT', 'EXCHANGE_ORDER', 'MANUAL_ORDER')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_payment_status",
                table: "sales_orders",
                sql: "payment_status IN ('UNPAID', 'PARTIALLY_PAID', 'PAID', 'PARTIALLY_REFUNDED', 'REFUNDED', 'FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_refunded_amount",
                table: "sales_orders",
                sql: "refunded_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_subtotal_amount",
                table: "sales_orders",
                sql: "subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_tax_amount",
                table: "sales_orders",
                sql: "tax_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_tenant_id_sales_order_id",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_tenant_id_sales_order_line_id",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "sales_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_tenant_id_tax_class_id",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "tax_class_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_tenant_id_tax_jurisdiction_id",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "tax_jurisdiction_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_tenant_id_tax_rate_id",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "tax_rate_id" });

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_taxes_tenant_id_id",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_taxes_calculation_sequence",
                table: "sales_order_taxes",
                sql: "calculation_sequence > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_taxes_tax_rate_percent",
                table: "sales_order_taxes",
                sql: "tax_rate_percent >= 0 AND tax_rate_percent <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_taxes_taxable_amount",
                table: "sales_order_taxes",
                sql: "taxable_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_status_history_changed_by_tenant_user_id",
                table: "sales_order_status_history",
                column: "changed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_status_history_sales_order_id_sequence_number",
                table: "sales_order_status_history",
                columns: new[] { "tenant_id", "sales_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_status_history_tenant_id_id",
                table: "sales_order_status_history",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_status_history_status_type",
                table: "sales_order_status_history",
                sql: "status_type IN ('ORDER_STATUS', 'PAYMENT_STATUS', 'FULFILLMENT_STATUS')");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_tenant_id_price_list_item_id",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "price_list_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_tenant_id_product_id",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_tenant_id_product_variant_id",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_tenant_id_sales_order_id",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_uom_id",
                table: "sales_order_lines",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_lines_tenant_id_id",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_cancelled_quantity",
                table: "sales_order_lines",
                sql: "cancelled_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_fulfilled_quantity",
                table: "sales_order_lines",
                sql: "fulfilled_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_line_discount_amount",
                table: "sales_order_lines",
                sql: "line_discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_line_status",
                table: "sales_order_lines",
                sql: "line_status IN ('ACTIVE', 'PARTIALLY_FULFILLED', 'FULFILLED', 'CANCELLED', 'RETURNED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_line_subtotal_amount",
                table: "sales_order_lines",
                sql: "line_subtotal_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_line_tax_amount",
                table: "sales_order_lines",
                sql: "line_tax_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_original_unit_price",
                table: "sales_order_lines",
                sql: "original_unit_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_returned_quantity",
                table: "sales_order_lines",
                sql: "returned_quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_unit_price",
                table: "sales_order_lines",
                sql: "unit_price >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_status_history_changed_by_tenant_user_id",
                table: "sales_order_line_status_history",
                column: "changed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_line_status_history_sales_order_line_id_sequence_number",
                table: "sales_order_line_status_history",
                columns: new[] { "tenant_id", "sales_order_line_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_line_status_history_tenant_id_id",
                table: "sales_order_line_status_history",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_line_status_history_affected_quantity",
                table: "sales_order_line_status_history",
                sql: "affected_quantity IS NULL OR affected_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_options_tenant_id_product_choice_group_id",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "product_choice_group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_options_tenant_id_product_choice_option_id",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "product_choice_option_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_options_tenant_id_sales_order_line_id",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "sales_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_line_options_tenant_id_id",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_item_uom_id",
                table: "sales_order_line_components",
                column: "item_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_tenant_id_combo_component_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "combo_component_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_tenant_id_combo_definition_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "combo_definition_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_tenant_id_combo_group_item_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "combo_group_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_tenant_id_item_product_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "item_product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_tenant_id_item_variant_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "item_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_tenant_id_sales_order_line_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "sales_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_line_components_tenant_id_id",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_line_components_item_source_type",
                table: "sales_order_line_components",
                sql: "item_source_type IN ('FIXED_COMPONENT', 'GROUP_ITEM')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_line_components_source_rules",
                table: "sales_order_line_components",
                sql: "(item_source_type = 'FIXED_COMPONENT' AND combo_component_id IS NOT NULL AND combo_group_item_id IS NULL) OR (item_source_type = 'GROUP_ITEM' AND combo_group_item_id IS NOT NULL AND combo_component_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_applied_by_tenant_user_id",
                table: "sales_order_discounts",
                column: "applied_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_discount_type_id",
                table: "sales_order_discounts",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_tenant_id_discount_policy_id",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "discount_policy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_tenant_id_sales_order_id",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_tenant_id_sales_order_line_id",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "sales_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_discounts_tenant_id_id",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_discounts_discount_target_scope",
                table: "sales_order_discounts",
                sql: "discount_target_scope IN ('ORDER', 'LINE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_discounts_discount_value",
                table: "sales_order_discounts",
                sql: "discount_value >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_fulfillment_method_outlets_tenant_id_id",
                table: "fulfillment_method_outlets",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_number_sequences_created_by_tenant_user_id",
                table: "document_number_sequences",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_number_sequences_tenant_id_outlet_id",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_document_number_sequences_tenant_id_sales_channel_id",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_document_number_sequences_updated_by_tenant_user_id",
                table: "document_number_sequences",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_document_number_sequences_document_type",
                table: "document_number_sequences",
                sql: "document_type IN ('SALES_ORDER', 'RECEIPT', 'PAYMENT', 'RETURN', 'REFUND', 'EXCHANGE', 'FULFILLMENT', 'PICKUP', 'INVOICE', 'CREDIT_NOTE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_document_number_sequences_reset_rule",
                table: "document_number_sequences",
                sql: "reset_rule IN ('NONE', 'DAILY', 'MONTHLY', 'YEARLY')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_document_number_sequences_row_version",
                table: "document_number_sequences",
                sql: "row_version >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_document_number_sequences_status",
                table: "document_number_sequences",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_applied_by_tenant_user_id",
                table: "sales_order_charges",
                column: "applied_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_tenant_id_sales_order_id",
                table: "sales_order_charges",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_tenant_id_sales_order_line_id",
                table: "sales_order_charges",
                columns: new[] { "tenant_id", "sales_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_charges_tenant_id_id",
                table: "sales_order_charges",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_document_number_sequences_created_by_tenant_user_id_tenant_users",
                table: "document_number_sequences",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_document_number_sequences_outlet_id_outlets",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_document_number_sequences_sales_channel_id_sales_channels",
                table: "document_number_sequences",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_document_number_sequences_tenant_id_tenants",
                table: "document_number_sequences",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_document_number_sequences_updated_by_tenant_user_id_tenant_users",
                table: "document_number_sequences",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_applied_by_tenant_user_id_tenant_users",
                table: "sales_order_discounts",
                column: "applied_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_discount_policy_id_discount_policies",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "discount_policy_id" },
                principalTable: "discount_policies",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_discount_type_id_discount_types",
                table: "sales_order_discounts",
                column: "discount_type_id",
                principalTable: "discount_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_sales_order_id_sales_orders",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "sales_order_id" },
                principalTable: "sales_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_sales_order_line_id_sales_order_lines",
                table: "sales_order_discounts",
                columns: new[] { "tenant_id", "sales_order_line_id" },
                principalTable: "sales_order_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_tenant_id_tenants",
                table: "sales_order_discounts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_combo_component_id_combo_components",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "combo_component_id" },
                principalTable: "combo_components",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_combo_definition_id_combo_definitions",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "combo_definition_id" },
                principalTable: "combo_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_combo_group_item_id_combo_group_items",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "combo_group_item_id" },
                principalTable: "combo_group_items",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_item_product_id_products",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "item_product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_item_uom_id_unit_of_measures",
                table: "sales_order_line_components",
                column: "item_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_item_variant_id_product_variants",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "item_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_components",
                columns: new[] { "tenant_id", "sales_order_line_id" },
                principalTable: "sales_order_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_tenant_id_tenants",
                table: "sales_order_line_components",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_options_product_choice_group_id_product_choice_groups",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "product_choice_group_id" },
                principalTable: "product_choice_groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_options_product_choice_option_id_product_choice_options",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "product_choice_option_id" },
                principalTable: "product_choice_options",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_options_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_options",
                columns: new[] { "tenant_id", "sales_order_line_id" },
                principalTable: "sales_order_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_options_tenant_id_tenants",
                table: "sales_order_line_options",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_status_history_changed_by_tenant_user_id_tenant_users",
                table: "sales_order_line_status_history",
                column: "changed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_status_history_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_status_history",
                columns: new[] { "tenant_id", "sales_order_line_id" },
                principalTable: "sales_order_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_status_history_tenant_id_tenants",
                table: "sales_order_line_status_history",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_price_list_item_id_price_list_items",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "price_list_item_id" },
                principalTable: "price_list_items",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_product_id_products",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_product_variant_id_product_variants",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_sales_order_id_sales_orders",
                table: "sales_order_lines",
                columns: new[] { "tenant_id", "sales_order_id" },
                principalTable: "sales_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_tenant_id_tenants",
                table: "sales_order_lines",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_uom_id_unit_of_measures",
                table: "sales_order_lines",
                column: "uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_status_history_changed_by_tenant_user_id_tenant_users",
                table: "sales_order_status_history",
                column: "changed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_status_history_sales_order_id_sales_orders",
                table: "sales_order_status_history",
                columns: new[] { "tenant_id", "sales_order_id" },
                principalTable: "sales_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_status_history_tenant_id_tenants",
                table: "sales_order_status_history",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_sales_order_id_sales_orders",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "sales_order_id" },
                principalTable: "sales_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_sales_order_line_id_sales_order_lines",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "sales_order_line_id" },
                principalTable: "sales_order_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_tax_class_id_tax_classes",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "tax_class_id" },
                principalTable: "tax_classes",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_tax_jurisdiction_id_tax_jurisdictions",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "tax_jurisdiction_id" },
                principalTable: "tax_jurisdictions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_tax_rate_id_tax_rates",
                table: "sales_order_taxes",
                columns: new[] { "tenant_id", "tax_rate_id" },
                principalTable: "tax_rates",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_tenant_id_tenants",
                table: "sales_order_taxes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_created_by_tenant_user_id_tenant_users",
                table: "sales_orders",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_customer_id_customers",
                table: "sales_orders",
                columns: new[] { "tenant_id", "customer_id" },
                principalTable: "customers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_document_number_sequence_id_document_number_sequences",
                table: "sales_orders",
                columns: new[] { "tenant_id", "document_number_sequence_id" },
                principalTable: "document_number_sequences",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_fulfillment_method_outlet_id_fulfillment_method_outlets",
                table: "sales_orders",
                columns: new[] { "tenant_id", "fulfillment_method_outlet_id" },
                principalTable: "fulfillment_method_outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_price_list_id_price_lists",
                table: "sales_orders",
                columns: new[] { "tenant_id", "price_list_id" },
                principalTable: "price_lists",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_sales_channel_id_sales_channels",
                table: "sales_orders",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_till_id_tills",
                table: "sales_orders",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_till_session_id_till_sessions",
                table: "sales_orders",
                columns: new[] { "tenant_id", "till_session_id" },
                principalTable: "till_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_updated_by_tenant_user_id_tenant_users",
                table: "sales_orders",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_number_sequences_created_by_tenant_user_id_tenant_users",
                table: "document_number_sequences");

            migrationBuilder.DropForeignKey(
                name: "fk_document_number_sequences_outlet_id_outlets",
                table: "document_number_sequences");

            migrationBuilder.DropForeignKey(
                name: "fk_document_number_sequences_sales_channel_id_sales_channels",
                table: "document_number_sequences");

            migrationBuilder.DropForeignKey(
                name: "fk_document_number_sequences_tenant_id_tenants",
                table: "document_number_sequences");

            migrationBuilder.DropForeignKey(
                name: "fk_document_number_sequences_updated_by_tenant_user_id_tenant_users",
                table: "document_number_sequences");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_applied_by_tenant_user_id_tenant_users",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_discount_policy_id_discount_policies",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_discount_type_id_discount_types",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_sales_order_id_sales_orders",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_sales_order_line_id_sales_order_lines",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_tenant_id_tenants",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_combo_component_id_combo_components",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_combo_definition_id_combo_definitions",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_combo_group_item_id_combo_group_items",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_item_product_id_products",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_item_uom_id_unit_of_measures",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_item_variant_id_product_variants",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_components_tenant_id_tenants",
                table: "sales_order_line_components");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_options_product_choice_group_id_product_choice_groups",
                table: "sales_order_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_options_product_choice_option_id_product_choice_options",
                table: "sales_order_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_options_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_options_tenant_id_tenants",
                table: "sales_order_line_options");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_status_history_changed_by_tenant_user_id_tenant_users",
                table: "sales_order_line_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_status_history_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_line_status_history_tenant_id_tenants",
                table: "sales_order_line_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_price_list_item_id_price_list_items",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_product_id_products",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_product_variant_id_product_variants",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_sales_order_id_sales_orders",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_tenant_id_tenants",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_lines_uom_id_unit_of_measures",
                table: "sales_order_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_status_history_changed_by_tenant_user_id_tenant_users",
                table: "sales_order_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_status_history_sales_order_id_sales_orders",
                table: "sales_order_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_status_history_tenant_id_tenants",
                table: "sales_order_status_history");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_sales_order_id_sales_orders",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_sales_order_line_id_sales_order_lines",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_tax_class_id_tax_classes",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_tax_jurisdiction_id_tax_jurisdictions",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_tax_rate_id_tax_rates",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_taxes_tenant_id_tenants",
                table: "sales_order_taxes");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_created_by_tenant_user_id_tenant_users",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_customer_id_customers",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_document_number_sequence_id_document_number_sequences",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_fulfillment_method_outlet_id_fulfillment_method_outlets",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_price_list_id_price_lists",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_sales_channel_id_sales_channels",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_till_id_tills",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_till_session_id_till_sessions",
                table: "sales_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_updated_by_tenant_user_id_tenant_users",
                table: "sales_orders");

            migrationBuilder.DropTable(
                name: "sales_order_charges");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_created_by_tenant_user_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_customer_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_document_number_sequence_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_fulfillment_method_outlet_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_price_list_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_sales_channel_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_till_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_till_session_id",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_updated_by_tenant_user_id",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_charge_amount",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_discount_amount",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_fulfillment_status",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_order_status",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_order_type",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_payment_status",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_refunded_amount",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_subtotal_amount",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_tax_amount",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_tenant_id_sales_order_id",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_tenant_id_sales_order_line_id",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_tenant_id_tax_class_id",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_tenant_id_tax_jurisdiction_id",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_taxes_tenant_id_tax_rate_id",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_taxes_tenant_id_id",
                table: "sales_order_taxes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_taxes_calculation_sequence",
                table: "sales_order_taxes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_taxes_tax_rate_percent",
                table: "sales_order_taxes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_taxes_taxable_amount",
                table: "sales_order_taxes");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_status_history_changed_by_tenant_user_id",
                table: "sales_order_status_history");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_status_history_sales_order_id_sequence_number",
                table: "sales_order_status_history");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_status_history_tenant_id_id",
                table: "sales_order_status_history");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_status_history_status_type",
                table: "sales_order_status_history");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_sales_order_lines_tenant_id_id",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_lines_tenant_id_price_list_item_id",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_lines_tenant_id_product_id",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_lines_tenant_id_product_variant_id",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_lines_tenant_id_sales_order_id",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_lines_uom_id",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_lines_tenant_id_id",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_cancelled_quantity",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_fulfilled_quantity",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_line_discount_amount",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_line_status",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_line_subtotal_amount",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_line_tax_amount",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_original_unit_price",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_returned_quantity",
                table: "sales_order_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_unit_price",
                table: "sales_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_status_history_changed_by_tenant_user_id",
                table: "sales_order_line_status_history");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_line_status_history_sales_order_line_id_sequence_number",
                table: "sales_order_line_status_history");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_line_status_history_tenant_id_id",
                table: "sales_order_line_status_history");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_line_status_history_affected_quantity",
                table: "sales_order_line_status_history");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_options_tenant_id_product_choice_group_id",
                table: "sales_order_line_options");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_options_tenant_id_product_choice_option_id",
                table: "sales_order_line_options");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_options_tenant_id_sales_order_line_id",
                table: "sales_order_line_options");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_line_options_tenant_id_id",
                table: "sales_order_line_options");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_item_uom_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_tenant_id_combo_component_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_tenant_id_combo_definition_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_tenant_id_combo_group_item_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_tenant_id_item_product_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_tenant_id_item_variant_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_line_components_tenant_id_sales_order_line_id",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_line_components_tenant_id_id",
                table: "sales_order_line_components");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_line_components_item_source_type",
                table: "sales_order_line_components");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_line_components_source_rules",
                table: "sales_order_line_components");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_applied_by_tenant_user_id",
                table: "sales_order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_discount_type_id",
                table: "sales_order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_tenant_id_discount_policy_id",
                table: "sales_order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_tenant_id_sales_order_id",
                table: "sales_order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_tenant_id_sales_order_line_id",
                table: "sales_order_discounts");

            migrationBuilder.DropIndex(
                name: "uq_sales_order_discounts_tenant_id_id",
                table: "sales_order_discounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_discounts_discount_target_scope",
                table: "sales_order_discounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_discounts_discount_value",
                table: "sales_order_discounts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_price_list_items_tenant_id_id",
                table: "price_list_items");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_fulfillment_method_outlets_tenant_id_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "uq_fulfillment_method_outlets_tenant_id_id",
                table: "fulfillment_method_outlets");

            migrationBuilder.DropIndex(
                name: "IX_document_number_sequences_created_by_tenant_user_id",
                table: "document_number_sequences");

            migrationBuilder.DropIndex(
                name: "IX_document_number_sequences_tenant_id_outlet_id",
                table: "document_number_sequences");

            migrationBuilder.DropIndex(
                name: "IX_document_number_sequences_tenant_id_sales_channel_id",
                table: "document_number_sequences");

            migrationBuilder.DropIndex(
                name: "IX_document_number_sequences_updated_by_tenant_user_id",
                table: "document_number_sequences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_document_number_sequences_document_type",
                table: "document_number_sequences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_document_number_sequences_reset_rule",
                table: "document_number_sequences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_document_number_sequences_row_version",
                table: "document_number_sequences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_document_number_sequences_status",
                table: "document_number_sequences");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_combo_group_items_tenant_id_id",
                table: "combo_group_items");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_combo_components_tenant_id_id",
                table: "combo_components");

            migrationBuilder.DropColumn(
                name: "balance_due",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "charge_amount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "customer_email_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "customer_name_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "customer_note",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "customer_phone_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "external_order_reference",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_method_code_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_method_outlet_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_status",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "internal_note",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "order_type",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "placed_at",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "rounding_amount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "sales_channel_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "till_session_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "calculation_sequence",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "is_tax_included",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "jurisdiction_name_snapshot",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tax_class_code_snapshot",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tax_class_id",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tax_jurisdiction_id",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tax_name_snapshot",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tax_rate_code_snapshot",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tax_rate_id",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "taxable_amount",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_taxes");

            migrationBuilder.DropColumn(
                name: "change_reason",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "changed_at",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "changed_by_tenant_user_id",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "status_type",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_status_history");

            migrationBuilder.DropColumn(
                name: "cancelled_quantity",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "fulfilled_quantity",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "line_discount_amount",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "line_status",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "line_subtotal_amount",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "line_tax_amount",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "original_unit_price",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "price_list_item_id",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "product_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "product_structure_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "product_type_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "returned_quantity",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "sku_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "uom_code_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "uom_id",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "uom_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "variant_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "affected_quantity",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "change_reason",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "changed_at",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "changed_by_tenant_user_id",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_line_status_history");

            migrationBuilder.DropColumn(
                name: "choice_group_name_snapshot",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "choice_option_name_snapshot",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "product_choice_group_id",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "product_choice_option_id",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "total_price_adjustment",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "unit_price_adjustment",
                table: "sales_order_line_options");

            migrationBuilder.DropColumn(
                name: "combo_component_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "combo_definition_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "combo_group_item_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_name_snapshot",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_product_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_sku_snapshot",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_source_type",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_uom_code_snapshot",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_uom_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_uom_name_snapshot",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_variant_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "item_variant_name_snapshot",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "total_price_adjustment",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "unit_price_adjustment",
                table: "sales_order_line_components");

            migrationBuilder.DropColumn(
                name: "applied_at",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "applied_by_tenant_user_id",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "calculation_method_snapshot",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "discount_code_snapshot",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "discount_name_snapshot",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "discount_policy_id",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "discount_target_scope",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "discount_type_id",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "discount_value",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "manual_discount_reason",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "last_generated_at",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "last_reset_at",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "prefix",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "reset_rule",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "status",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "suffix",
                table: "document_number_sequences");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "document_number_sequences");

            migrationBuilder.RenameColumn(
                name: "order_status",
                table: "sales_orders",
                newName: "status");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "sales_orders",
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
                name: "paid_amount",
                table: "sales_orders",
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
                name: "status",
                table: "sales_orders",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_rate_percent",
                table: "sales_order_taxes",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,4)",
                oldPrecision: 7,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "sales_order_taxes",
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
                table: "sales_order_taxes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_order_status_history",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total_amount",
                table: "sales_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "line_number",
                table: "sales_order_lines",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_order_line_status_history",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "sales_order_line_options",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_order_line_options",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "sales_order_line_options",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 1m);

            migrationBuilder.AlterColumn<int>(
                name: "sort_order",
                table: "sales_order_line_components",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_line_id",
                table: "sales_order_line_components",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_order_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "sales_order_discounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "application_sequence",
                table: "sales_order_discounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_channel_id",
                table: "document_number_sequences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "padding_length",
                table: "document_number_sequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 6);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "document_number_sequences",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "document_subtype",
                table: "document_number_sequences",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "current_value",
                table: "document_number_sequences",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_sales_order_id",
                table: "sales_order_taxes",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_taxes_sales_order_line_id",
                table: "sales_order_taxes",
                column: "sales_order_line_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_taxes_tax_rate_percent",
                table: "sales_order_taxes",
                sql: "tax_rate_percent >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_status_history_sales_order_id_sequence_number",
                table: "sales_order_status_history",
                columns: new[] { "sales_order_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_line_status_history_sales_order_line_id_sequence_number",
                table: "sales_order_line_status_history",
                columns: new[] { "sales_order_line_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_options_sales_order_line_id",
                table: "sales_order_line_options",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_line_components_sales_order_line_id",
                table: "sales_order_line_components",
                column: "sales_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_sales_order_id",
                table: "sales_order_discounts",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_sales_order_line_id",
                table: "sales_order_discounts",
                column: "sales_order_line_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_sales_order_id_sales_orders",
                table: "sales_order_discounts",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_sales_order_line_id_sales_order_lines",
                table: "sales_order_discounts",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_components_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_components",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_options_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_options",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_line_status_history_sales_order_line_id_sales_order_lines",
                table: "sales_order_line_status_history",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_lines_sales_order_id_sales_orders",
                table: "sales_order_lines",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_status_history_sales_order_id_sales_orders",
                table: "sales_order_status_history",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_sales_order_id_sales_orders",
                table: "sales_order_taxes",
                column: "sales_order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_taxes_sales_order_line_id_sales_order_lines",
                table: "sales_order_taxes",
                column: "sales_order_line_id",
                principalTable: "sales_order_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
