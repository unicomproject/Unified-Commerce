using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletManagerAndMediaMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "media_asset_id",
                table: "outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary_manager",
                table: "outlet_user_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_outlets_tenant_id_media_asset_id",
                table: "outlets",
                columns: new[] { "tenant_id", "media_asset_id" });

            migrationBuilder.CreateIndex(
                name: "uq_outlet_user_roles_tenant_outlet_primary_manager",
                table: "outlet_user_roles",
                columns: new[] { "tenant_id", "outlet_id" },
                unique: true,
                filter: "is_primary_manager = true AND revoked_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_outlets_media_asset_tenant",
                table: "outlets",
                columns: new[] { "tenant_id", "media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outlets_media_asset_tenant",
                table: "outlets");

            migrationBuilder.DropIndex(
                name: "uq_outlet_user_roles_tenant_outlet_primary_manager",
                table: "outlet_user_roles");

            migrationBuilder.DropIndex(
                name: "ix_outlets_tenant_id_media_asset_id",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "is_primary_manager",
                table: "outlet_user_roles");

            migrationBuilder.DropColumn(
                name: "media_asset_id",
                table: "outlets");
        }
    }
}
