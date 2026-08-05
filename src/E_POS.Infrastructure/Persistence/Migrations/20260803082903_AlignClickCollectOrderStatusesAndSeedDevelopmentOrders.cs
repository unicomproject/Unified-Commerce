using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignClickCollectOrderStatusesAndSeedDevelopmentOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_fulfillment_status",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_order_status",
                table: "sales_orders");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_fulfillment_status",
                table: "sales_orders",
                sql: "fulfillment_status IN ('NOT_REQUIRED', 'PENDING', 'ACCEPTED', 'PREPARING', 'READY', 'READY_FOR_PICKUP', 'READY_FOR_COLLECTION', 'PARTIALLY_FULFILLED', 'FULFILLED', 'COLLECTED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_order_status",
                table: "sales_orders",
                sql: "order_status IN ('DRAFT', 'PLACED', 'CONFIRMED', 'ACCEPTED', 'COMPLETED', 'CANCELLED', 'VOIDED')");

            migrationBuilder.Sql(DevelopmentClickCollectOrderStatusSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentClickCollectOrderStatusSeedData.DownSql);

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_fulfillment_status",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_order_status",
                table: "sales_orders");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_fulfillment_status",
                table: "sales_orders",
                sql: "fulfillment_status IN ('NOT_REQUIRED', 'PENDING', 'READY_FOR_PICKUP', 'PARTIALLY_FULFILLED', 'FULFILLED', 'CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_order_status",
                table: "sales_orders",
                sql: "order_status IN ('DRAFT', 'PLACED', 'CONFIRMED', 'COMPLETED', 'CANCELLED', 'VOIDED')");
        }
    }
}
