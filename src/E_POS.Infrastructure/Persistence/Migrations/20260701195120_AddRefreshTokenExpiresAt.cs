using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenExpiresAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "tenant_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "platform_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE tenant_refresh_tokens SET expires_at = created_at + INTERVAL '7 days' WHERE expires_at IS NULL;");
            migrationBuilder.Sql("UPDATE platform_refresh_tokens SET expires_at = created_at + INTERVAL '7 days' WHERE expires_at IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "tenant_refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "platform_refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_refresh_tokens_expires_at_created_at",
                table: "tenant_refresh_tokens",
                sql: "expires_at > created_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_refresh_tokens_expires_at_created_at",
                table: "platform_refresh_tokens",
                sql: "expires_at > created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_refresh_tokens_expires_at_created_at",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_refresh_tokens_expires_at_created_at",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "tenant_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "platform_refresh_tokens");
        }
    }
}