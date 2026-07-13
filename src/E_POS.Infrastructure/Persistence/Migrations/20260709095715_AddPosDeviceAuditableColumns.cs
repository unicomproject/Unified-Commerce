using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosDeviceAuditableColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "pos_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "pos_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_created_by_tenant_user_id",
                table: "pos_devices",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_devices_updated_by_tenant_user_id",
                table: "pos_devices",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_devices_created_by_tenant_user_id_tenant_users",
                table: "pos_devices",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pos_devices_updated_by_tenant_user_id_tenant_users",
                table: "pos_devices",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pos_devices_created_by_tenant_user_id_tenant_users",
                table: "pos_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_pos_devices_updated_by_tenant_user_id_tenant_users",
                table: "pos_devices");

            migrationBuilder.DropIndex(
                name: "IX_pos_devices_created_by_tenant_user_id",
                table: "pos_devices");

            migrationBuilder.DropIndex(
                name: "IX_pos_devices_updated_by_tenant_user_id",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "pos_devices");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "pos_devices");
        }
    }
}
