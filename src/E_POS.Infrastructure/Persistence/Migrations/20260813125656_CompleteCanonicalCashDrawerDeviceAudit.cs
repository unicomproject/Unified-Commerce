using System;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCanonicalCashDrawerDeviceAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_till_cash_movements_tenant_id_till_session_id",
                table: "till_cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_cash_movements_movement_type",
                table: "till_cash_movements");

            migrationBuilder.AddColumn<Guid>(
                name: "pos_device_id",
                table: "till_cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "request_id",
                table: "till_cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_till_cash_movements_session_performed_at",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "till_session_id", "performed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_till_cash_movements_tenant_id_pos_device_id",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "uq_till_cash_movements_tenant_request_id",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "request_id" },
                unique: true,
                filter: "request_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_cash_movements_movement_type",
                table: "till_cash_movements",
                sql: "movement_type IN ('CASH_IN', 'CASH_OUT', 'CASH_DROP', 'OPENING_FLOAT', 'CLOSING_REMOVE')");

            migrationBuilder.AddForeignKey(
                name: "fk_till_cash_movements_pos_device_id_pos_devices",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(DevelopmentPosCashDrawerPermissionsSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosCashDrawerPermissionsSeedData.CashierAssignmentUpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosCashDrawerPermissionsSeedData.CashierAssignmentDownSql);
            migrationBuilder.Sql(DevelopmentPosCashDrawerPermissionsSeedData.DownSql);

            migrationBuilder.DropForeignKey(
                name: "fk_till_cash_movements_pos_device_id_pos_devices",
                table: "till_cash_movements");

            migrationBuilder.DropIndex(
                name: "ix_till_cash_movements_session_performed_at",
                table: "till_cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_till_cash_movements_tenant_id_pos_device_id",
                table: "till_cash_movements");

            migrationBuilder.DropIndex(
                name: "uq_till_cash_movements_tenant_request_id",
                table: "till_cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_cash_movements_movement_type",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "pos_device_id",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "till_cash_movements");

            migrationBuilder.CreateIndex(
                name: "IX_till_cash_movements_tenant_id_till_session_id",
                table: "till_cash_movements",
                columns: new[] { "tenant_id", "till_session_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_cash_movements_movement_type",
                table: "till_cash_movements",
                sql: "movement_type IN ('CASH_IN', 'CASH_OUT', 'OPENING_FLOAT', 'CLOSING_REMOVE')");
        }
    }
}
