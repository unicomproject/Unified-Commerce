using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncCapabilityRegistryModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "platform_feature_id",
                table: "platform_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "platform_module_id",
                table: "platform_permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "platform_modules",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TENANT");

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "platform_features",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TENANT");

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "permission_definitions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TENANT");

            migrationBuilder.CreateIndex(
                name: "IX_platform_permissions_platform_feature_id",
                table: "platform_permissions",
                column: "platform_feature_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_permissions_platform_module_id",
                table: "platform_permissions",
                column: "platform_module_id");

            migrationBuilder.AddForeignKey(
                name: "fk_platform_permissions_platform_feature_id_platform_features",
                table: "platform_permissions",
                column: "platform_feature_id",
                principalTable: "platform_features",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_permissions_platform_module_id_platform_modules",
                table: "platform_permissions",
                column: "platform_module_id",
                principalTable: "platform_modules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_platform_permissions_platform_feature_id_platform_features",
                table: "platform_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_permissions_platform_module_id_platform_modules",
                table: "platform_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_permissions_platform_feature_id",
                table: "platform_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_permissions_platform_module_id",
                table: "platform_permissions");

            migrationBuilder.DropColumn(
                name: "platform_feature_id",
                table: "platform_permissions");

            migrationBuilder.DropColumn(
                name: "platform_module_id",
                table: "platform_permissions");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "platform_modules");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "platform_features");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "permission_definitions");
        }
    }
}
