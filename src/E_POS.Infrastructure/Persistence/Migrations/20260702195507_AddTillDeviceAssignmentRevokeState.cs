using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTillDeviceAssignmentRevokeState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_till_device_assignments_pos_device_id",
                table: "till_device_assignments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_to",
                table: "till_device_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "till_device_assignments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_active_pos_device_id",
                table: "till_device_assignments",
                column: "pos_device_id",
                unique: true,
                filter: "status = 'ACTIVE' AND pos_device_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_till_device_assignments_active_till_id_pos_device_id",
                table: "till_device_assignments",
                columns: new[] { "till_id", "pos_device_id" },
                unique: true,
                filter: "status = 'ACTIVE' AND till_id IS NOT NULL AND pos_device_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_device_assignments_status",
                table: "till_device_assignments",
                sql: "status IN ('ACTIVE', 'REVOKED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_active_pos_device_id",
                table: "till_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_till_device_assignments_active_till_id_pos_device_id",
                table: "till_device_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_device_assignments_status",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "effective_to",
                table: "till_device_assignments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "till_device_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_till_device_assignments_pos_device_id",
                table: "till_device_assignments",
                column: "pos_device_id");
        }
    }
}
