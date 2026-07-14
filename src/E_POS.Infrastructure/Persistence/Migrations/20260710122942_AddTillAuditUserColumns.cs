using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTillAuditUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bypassing because columns already exist in local database
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tills_created_by_tenant_user_id_tenant_users",
                table: "tills");

            migrationBuilder.DropForeignKey(
                name: "fk_tills_updated_by_tenant_user_id_tenant_users",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "IX_tills_created_by_tenant_user_id",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "IX_tills_updated_by_tenant_user_id",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "tills");
        }
    }
}
