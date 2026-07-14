using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletTillDeviceAuditUserColumns : Migration
    {
        // The model snapshot already declared created/updated audit user columns
        // for these tables, but no migration ever created them in the database.
        private static readonly string[] Tables =
        [
            "pos_devices",
            "outlet_addresses",
            "hardware_profiles",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "created_by_tenant_user_id",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.AddColumn<Guid>(
                    name: "updated_by_tenant_user_id",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.CreateIndex(
                    name: $"IX_{table}_created_by_tenant_user_id",
                    table: table,
                    column: "created_by_tenant_user_id");

                migrationBuilder.CreateIndex(
                    name: $"IX_{table}_updated_by_tenant_user_id",
                    table: table,
                    column: "updated_by_tenant_user_id");

                migrationBuilder.AddForeignKey(
                    name: $"fk_{table}_created_by_tenant_user_id_tenant_users",
                    table: table,
                    column: "created_by_tenant_user_id",
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: $"fk_{table}_updated_by_tenant_user_id_tenant_users",
                    table: table,
                    column: "updated_by_tenant_user_id",
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropForeignKey(
                    name: $"fk_{table}_created_by_tenant_user_id_tenant_users",
                    table: table);

                migrationBuilder.DropForeignKey(
                    name: $"fk_{table}_updated_by_tenant_user_id_tenant_users",
                    table: table);

                migrationBuilder.DropIndex(
                    name: $"IX_{table}_created_by_tenant_user_id",
                    table: table);

                migrationBuilder.DropIndex(
                    name: $"IX_{table}_updated_by_tenant_user_id",
                    table: table);

                migrationBuilder.DropColumn(
                    name: "created_by_tenant_user_id",
                    table: table);

                migrationBuilder.DropColumn(
                    name: "updated_by_tenant_user_id",
                    table: table);
            }
        }
    }
}
