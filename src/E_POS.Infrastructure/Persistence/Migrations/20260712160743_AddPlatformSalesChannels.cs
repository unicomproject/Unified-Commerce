using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSalesChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.DropIndex(
                name: "ix_sales_channels_tenant_id_channel_code",
                table: "sales_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_channels_channel_mode",
                table: "sales_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_channels_channel_type",
                table: "sales_channels");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.DropColumn(
                name: "channel_code",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "channel_mode",
                table: "sales_channels");

            migrationBuilder.DropColumn(
                name: "channel_type",
                table: "sales_channels");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.RenameColumn(
                name: "channel_name",
                table: "sales_channels",
                newName: "custom_name");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.AddColumn<Guid>(
                name: "platform_sales_channel_id",
                table: "sales_channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.CreateTable(
                name: "platform_sales_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    default_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    channel_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_sales_channels", x => x.id);
                    table.CheckConstraint("ck_platform_sales_channels_channel_type", "channel_type IN ('PHYSICAL', 'ONLINE', 'AGGREGATOR', 'B2B', 'OTHER')");
                });
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.CreateIndex(
                name: "IX_sales_channels_platform_sales_channel_id",
                table: "sales_channels",
                column: "platform_sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_channels_tenant_id_platform_channel_id",
                table: "sales_channels",
                columns: new[] { "tenant_id", "platform_sales_channel_id" },
                unique: true);
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.CreateIndex(
                name: "ix_platform_sales_channels_channel_code",
                table: "platform_sales_channels",
                column: "channel_code",
                unique: true);
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.AddForeignKey(
                name: "fk_sales_channels_platform_sales_channel_id",
                table: "sales_channels",
                column: "platform_sales_channel_id",
                principalTable: "platform_sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.DropForeignKey(
                name: "fk_sales_channels_platform_sales_channel_id",
                table: "sales_channels");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.DropTable(
                name: "platform_sales_channels");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.DropIndex(
                name: "IX_sales_channels_platform_sales_channel_id",
                table: "sales_channels");

            migrationBuilder.DropIndex(
                name: "ix_sales_channels_tenant_id_platform_channel_id",
                table: "sales_channels");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.DropColumn(
                name: "platform_sales_channel_id",
                table: "sales_channels");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.RenameColumn(
                name: "custom_name",
                table: "sales_channels",
                newName: "channel_name");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.AddColumn<string>(
                name: "channel_code",
                table: "sales_channels",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "channel_mode",
                table: "sales_channels",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channel_type",
                table: "sales_channels",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement

            migrationBuilder.CreateIndex(
                name: "ix_sales_channels_tenant_id_channel_code",
                table: "sales_channels",
                columns: new[] { "tenant_id", "channel_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_channels_channel_mode",
                table: "sales_channels",
                sql: "channel_mode IS NULL OR channel_mode IN ('ONLINE', 'OFFLINE', 'HYBRID')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_channels_channel_type",
                table: "sales_channels",
                sql: "channel_type IN ('POS', 'E_COMMERCE')");
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
            // Removed unneeded statement
        }
    }
}

