using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAdminReportsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_movements_tenant_id_inventory_balance_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_tenant_id_sales_channel_id",
                table: "sales_orders");

            migrationBuilder.AddColumn<string>(
                name: "reason_code",
                table: "stock_movements",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_number_snapshot",
                table: "stock_movements",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_outlet_code_snapshot",
                table: "sales_returns",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_outlet_name_snapshot",
                table: "sales_returns",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_reason_code_snapshot",
                table: "sales_returns",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_reason_name_snapshot",
                table: "sales_returns",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_approved_qty",
                table: "sales_returns",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_approved",
                table: "sales_return_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_reason_code_snapshot",
                table: "sales_return_lines",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_reason_name_snapshot",
                table: "sales_return_lines",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "sales_refund_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "business_date",
                table: "sales_orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reporting_outlet_code_snapshot",
                table: "sales_orders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reporting_outlet_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reporting_outlet_name_snapshot",
                table: "sales_orders",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "barcode_snapshot",
                table: "sales_order_lines",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subcategory_name_snapshot",
                table: "sales_order_lines",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "approval_required_snapshot",
                table: "sales_order_discounts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "sales_order_discounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_tenant_user_id",
                table: "sales_order_discounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_balance_occurred_at",
                table: "stock_movements",
                columns: new[] { "tenant_id", "inventory_balance_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_occurred_at_type",
                table: "stock_movements",
                columns: new[] { "tenant_id", "occurred_at", "movement_type" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_tenant_sales_order_id",
                table: "sales_returns",
                columns: new[] { "tenant_id", "sales_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_tenant_status_completed_at",
                table: "sales_returns",
                columns: new[] { "tenant_id", "return_status", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_business_date_status",
                table: "sales_orders",
                columns: new[] { "tenant_id", "business_date", "order_status" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_cashier_business_date",
                table: "sales_orders",
                columns: new[] { "tenant_id", "created_by_tenant_user_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_payment_status_business_date",
                table: "sales_orders",
                columns: new[] { "tenant_id", "payment_status", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_reporting_outlet_business_date",
                table: "sales_orders",
                columns: new[] { "tenant_id", "reporting_outlet_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_sales_channel_business_date",
                table: "sales_orders",
                columns: new[] { "tenant_id", "sales_channel_id", "business_date" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_discounts_approved_by_tenant_user_id",
                table: "sales_order_discounts",
                column: "approved_by_tenant_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_order_discounts_approved_by_tenant_user_id_tenant_users",
                table: "sales_order_discounts",
                column: "approved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_reporting_outlet_id_outlets",
                table: "sales_orders",
                columns: new[] { "tenant_id", "reporting_outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_order_discounts_approved_by_tenant_user_id_tenant_users",
                table: "sales_order_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_reporting_outlet_id_outlets",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_tenant_balance_occurred_at",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_tenant_occurred_at_type",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_tenant_sales_order_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_tenant_status_completed_at",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_sales_orders_tenant_business_date_status",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "ix_sales_orders_tenant_cashier_business_date",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "ix_sales_orders_tenant_payment_status_business_date",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "ix_sales_orders_tenant_reporting_outlet_business_date",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "ix_sales_orders_tenant_sales_channel_business_date",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_order_discounts_approved_by_tenant_user_id",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "reason_code",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "reference_number_snapshot",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "processing_outlet_code_snapshot",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "processing_outlet_name_snapshot",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "return_reason_code_snapshot",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "return_reason_name_snapshot",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "total_approved_qty",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "quantity_approved",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "return_reason_code_snapshot",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "return_reason_name_snapshot",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "sales_refund_lines");

            migrationBuilder.DropColumn(
                name: "business_date",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "reporting_outlet_code_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "reporting_outlet_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "reporting_outlet_name_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "barcode_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "brand_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "category_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "department_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "subcategory_name_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "approval_required_snapshot",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "sales_order_discounts");

            migrationBuilder.DropColumn(
                name: "approved_by_tenant_user_id",
                table: "sales_order_discounts");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_tenant_id_inventory_balance_id",
                table: "stock_movements",
                columns: new[] { "tenant_id", "inventory_balance_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_tenant_id_sales_channel_id",
                table: "sales_orders",
                columns: new[] { "tenant_id", "sales_channel_id" });
        }
    }
}
