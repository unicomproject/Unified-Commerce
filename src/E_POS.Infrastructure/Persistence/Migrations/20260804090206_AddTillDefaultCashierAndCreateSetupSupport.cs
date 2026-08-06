using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTillDefaultCashierAndCreateSetupSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tills_tenant_id_outlet_id_till_code",
                table: "tills");

            migrationBuilder.AddColumn<Guid>(
                name: "default_cashier_tenant_user_id",
                table: "tills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tills_default_cashier_tenant_user_id",
                table: "tills",
                column: "default_cashier_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tills_tenant_id_till_code",
                table: "tills",
                columns: new[] { "tenant_id", "till_code" },
                unique: true,
                filter: "status != 'DELETED'");

            migrationBuilder.AddForeignKey(
                name: "fk_tills_default_cashier_tenant_user_id_tenant_users",
                table: "tills",
                column: "default_cashier_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tills_default_cashier_tenant_user_id_tenant_users",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "IX_tills_default_cashier_tenant_user_id",
                table: "tills");

            migrationBuilder.DropIndex(
                name: "uq_tills_tenant_id_till_code",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "default_cashier_tenant_user_id",
                table: "tills");

            migrationBuilder.CreateIndex(
                name: "uq_tills_tenant_id_outlet_id_till_code",
                table: "tills",
                columns: new[] { "tenant_id", "outlet_id", "till_code" },
                unique: true);
        }
    }
}
