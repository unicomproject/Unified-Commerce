using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletAuditUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outlets_created_by_tenant_user_id",
                table: "outlets",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outlets_updated_by_tenant_user_id",
                table: "outlets",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_outlets_created_by_tenant_user_id_tenant_users",
                table: "outlets",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_outlets_updated_by_tenant_user_id_tenant_users",
                table: "outlets",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outlets_created_by_tenant_user_id_tenant_users",
                table: "outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_outlets_updated_by_tenant_user_id_tenant_users",
                table: "outlets");

            migrationBuilder.DropIndex(
                name: "IX_outlets_created_by_tenant_user_id",
                table: "outlets");

            migrationBuilder.DropIndex(
                name: "IX_outlets_updated_by_tenant_user_id",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "outlets");
        }
    }
}
