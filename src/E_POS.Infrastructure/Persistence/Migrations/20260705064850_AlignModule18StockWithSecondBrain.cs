using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule18StockWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "requested_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_completed_by_tenant_user_id",
                table: "stocktake_sessions",
                column: "completed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_posted_by_tenant_user_id",
                table: "stocktake_sessions",
                column: "posted_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_sessions_started_by_tenant_user_id",
                table: "stocktake_sessions",
                column: "started_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktake_lines_counted_by_tenant_user_id",
                table: "stocktake_lines",
                column: "counted_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_stocktake_line_serials_tenant_id_id",
                table: "stocktake_line_serials",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_approved_by_tenant_user_id",
                table: "stock_transfers",
                column: "approved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_cancelled_by_tenant_user_id",
                table: "stock_transfers",
                column: "cancelled_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_received_by_tenant_user_id",
                table: "stock_transfers",
                column: "received_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_shipped_by_tenant_user_id",
                table: "stock_transfers",
                column: "shipped_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_transfers_transfer_status",
                table: "stock_transfers",
                sql: "transfer_status IN ('REQUESTED', 'APPROVED', 'SHIPPED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CANCELLED')");

            migrationBuilder.CreateIndex(
                name: "uq_stock_transfer_status_history_tenant_id_id",
                table: "stock_transfer_status_history",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_approved_by_tenant_user_id_tenant_users",
                table: "stock_transfers",
                column: "approved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_cancelled_by_tenant_user_id_tenant_users",
                table: "stock_transfers",
                column: "cancelled_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_received_by_tenant_user_id_tenant_users",
                table: "stock_transfers",
                column: "received_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transfers_shipped_by_tenant_user_id_tenant_users",
                table: "stock_transfers",
                column: "shipped_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_lines_counted_by_tenant_user_id_tenant_users",
                table: "stocktake_lines",
                column: "counted_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_completed_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions",
                column: "completed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_posted_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions",
                column: "posted_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stocktake_sessions_started_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions",
                column: "started_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_approved_by_tenant_user_id_tenant_users",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_cancelled_by_tenant_user_id_tenant_users",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_received_by_tenant_user_id_tenant_users",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transfers_shipped_by_tenant_user_id_tenant_users",
                table: "stock_transfers");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_lines_counted_by_tenant_user_id_tenant_users",
                table: "stocktake_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_completed_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_posted_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_stocktake_sessions_started_by_tenant_user_id_tenant_users",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_completed_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_posted_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_sessions_started_by_tenant_user_id",
                table: "stocktake_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stocktake_lines_counted_by_tenant_user_id",
                table: "stocktake_lines");

            migrationBuilder.DropIndex(
                name: "uq_stocktake_line_serials_tenant_id_id",
                table: "stocktake_line_serials");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_approved_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_cancelled_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_received_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "IX_stock_transfers_shipped_by_tenant_user_id",
                table: "stock_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_transfers_transfer_status",
                table: "stock_transfers");

            migrationBuilder.DropIndex(
                name: "uq_stock_transfer_status_history_tenant_id_id",
                table: "stock_transfer_status_history");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "requested_at",
                table: "stock_transfers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }
    }
}
