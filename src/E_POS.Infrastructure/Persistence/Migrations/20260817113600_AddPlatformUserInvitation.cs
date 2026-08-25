using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformUserInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_users_status",
                table: "platform_users");

            migrationBuilder.AlterColumn<string>(
                name: "change_type",
                table: "tenant_subscription_history",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                table: "price_lists",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "price_includes_tax",
                table: "price_lists",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "is_default_price_list",
                table: "price_lists",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "platform_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateTable(
                name: "platform_user_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_user_invitations", x => x.id);
                    table.CheckConstraint("ck_platform_user_invitations_status", "status IN ('PENDING', 'SENT')");
                    table.ForeignKey(
                        name: "fk_platform_user_invitations_platform_user_id_platform_users",
                        column: x => x.platform_user_id,
                        principalTable: "platform_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_users_status",
                table: "platform_users",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'LOCKED', 'DELETED', 'INVITED')");

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_invitations_platform_user_id",
                table: "platform_user_invitations",
                column: "platform_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_platform_user_invitations_token_hash",
                table: "platform_user_invitations",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_user_invitations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_users_status",
                table: "platform_users");

            migrationBuilder.AlterColumn<string>(
                name: "change_type",
                table: "tenant_subscription_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                table: "price_lists",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "price_includes_tax",
                table: "price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_default_price_list",
                table: "price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "platform_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_users_status",
                table: "platform_users",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'LOCKED', 'DELETED')");
        }
    }
}
