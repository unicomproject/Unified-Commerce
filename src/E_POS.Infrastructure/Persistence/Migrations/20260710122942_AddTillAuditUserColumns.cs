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
            // The model snapshot already declared these columns for tills, but no
            // migration ever created them (AddOutletAuditUserColumns only covered
            // outlets), so they are added here explicitly.
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tills",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tills_created_by_tenant_user_id",
                table: "tills",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tills_updated_by_tenant_user_id",
                table: "tills",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tills_created_by_tenant_user_id_tenant_users",
                table: "tills",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tills_updated_by_tenant_user_id_tenant_users",
                table: "tills",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
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
