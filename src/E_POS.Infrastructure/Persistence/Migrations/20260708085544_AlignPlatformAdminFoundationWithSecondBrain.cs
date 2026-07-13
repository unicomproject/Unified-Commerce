using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPlatformAdminFoundationWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "platform_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "platform_users",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_verified_at",
                table: "platform_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failed_login_count",
                table: "platform_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "platform_users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job_title",
                table: "platform_users",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at",
                table: "platform_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "platform_users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                table: "platform_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_changed_at",
                table: "platform_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "platform_users",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "platform_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "platform_user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_platform_user_id",
                table: "platform_user_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "platform_user_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "platform_user_roles",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "assigned_at",
                table: "platform_user_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_platform_user_id",
                table: "platform_user_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_user_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "platform_user_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "platform_user_permissions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "platform_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_system_role",
                table: "platform_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "platform_roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "granted_at",
                table: "platform_role_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "granted_by_platform_user_id",
                table: "platform_role_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_role_permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "platform_role_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "platform_role_permissions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "platform_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "module_key",
                table: "platform_permissions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "platform_permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE platform_permissions
                SET module_key = CASE
                    WHEN permission_code LIKE 'platform.%.%' THEN split_part(permission_code, '.', 2)
                    ELSE 'unknown'
                END
                WHERE module_key = '';

                UPDATE platform_roles
                SET is_system_role = true
                WHERE role_code = 'super_administrator';

                UPDATE platform_user_roles
                SET assigned_at = created_at
                WHERE assigned_at IS NULL;

                UPDATE platform_user_permissions
                SET assigned_at = created_at
                WHERE assigned_at IS NULL;

                UPDATE platform_role_permissions
                SET granted_at = created_at
                WHERE granted_at IS NULL;

                UPDATE platform_users u
                SET last_login_at = audit.last_login_at
                FROM (
                    SELECT platform_user_id, MAX(created_at) AS last_login_at
                    FROM platform_login_audits
                    WHERE login_result = 'SUCCESS'
                      AND platform_user_id IS NOT NULL
                    GROUP BY platform_user_id
                ) audit
                WHERE u.id = audit.platform_user_id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_platform_users_created_by_platform_user_id",
                table: "platform_users",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_users_updated_by_platform_user_id",
                table: "platform_users",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_roles_assigned_by_platform_user_id",
                table: "platform_user_roles",
                column: "assigned_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_roles_revoked_by_platform_user_id",
                table: "platform_user_roles",
                column: "revoked_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_permissions_assigned_by_platform_user_id",
                table: "platform_user_permissions",
                column: "assigned_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_user_permissions_revoked_by_platform_user_id",
                table: "platform_user_permissions",
                column: "revoked_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_roles_created_by_platform_user_id",
                table: "platform_roles",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_roles_updated_by_platform_user_id",
                table: "platform_roles",
                column: "updated_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_role_permissions_granted_by_platform_user_id",
                table: "platform_role_permissions",
                column: "granted_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_role_permissions_revoked_by_platform_user_id",
                table: "platform_role_permissions",
                column: "revoked_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_permissions_created_by_platform_user_id",
                table: "platform_permissions",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_permissions_updated_by_platform_user_id",
                table: "platform_permissions",
                column: "updated_by_platform_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_platform_permissions_created_by_platform_user_id_platform_users",
                table: "platform_permissions",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_permissions_updated_by_platform_user_id_platform_users",
                table: "platform_permissions",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_role_permissions_granted_by_platform_user_id_platform_users",
                table: "platform_role_permissions",
                column: "granted_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_role_permissions_revoked_by_platform_user_id_platform_users",
                table: "platform_role_permissions",
                column: "revoked_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_roles_created_by_platform_user_id_platform_users",
                table: "platform_roles",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_roles_updated_by_platform_user_id_platform_users",
                table: "platform_roles",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_user_permissions_assigned_by_platform_user_id_platform_users",
                table: "platform_user_permissions",
                column: "assigned_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_user_permissions_revoked_by_platform_user_id_platform_users",
                table: "platform_user_permissions",
                column: "revoked_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_user_roles_assigned_by_platform_user_id_platform_users",
                table: "platform_user_roles",
                column: "assigned_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_user_roles_revoked_by_platform_user_id_platform_users",
                table: "platform_user_roles",
                column: "revoked_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_users_created_by_platform_user_id_platform_users",
                table: "platform_users",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_users_updated_by_platform_user_id_platform_users",
                table: "platform_users",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_platform_permissions_created_by_platform_user_id_platform_users",
                table: "platform_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_permissions_updated_by_platform_user_id_platform_users",
                table: "platform_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_role_permissions_granted_by_platform_user_id_platform_users",
                table: "platform_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_role_permissions_revoked_by_platform_user_id_platform_users",
                table: "platform_role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_roles_created_by_platform_user_id_platform_users",
                table: "platform_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_roles_updated_by_platform_user_id_platform_users",
                table: "platform_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_user_permissions_assigned_by_platform_user_id_platform_users",
                table: "platform_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_user_permissions_revoked_by_platform_user_id_platform_users",
                table: "platform_user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_user_roles_assigned_by_platform_user_id_platform_users",
                table: "platform_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_user_roles_revoked_by_platform_user_id_platform_users",
                table: "platform_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_users_created_by_platform_user_id_platform_users",
                table: "platform_users");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_users_updated_by_platform_user_id_platform_users",
                table: "platform_users");

            migrationBuilder.DropIndex(
                name: "IX_platform_users_created_by_platform_user_id",
                table: "platform_users");

            migrationBuilder.DropIndex(
                name: "IX_platform_users_updated_by_platform_user_id",
                table: "platform_users");

            migrationBuilder.DropIndex(
                name: "IX_platform_user_roles_assigned_by_platform_user_id",
                table: "platform_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_platform_user_roles_revoked_by_platform_user_id",
                table: "platform_user_roles");

            migrationBuilder.DropIndex(
                name: "IX_platform_user_permissions_assigned_by_platform_user_id",
                table: "platform_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_user_permissions_revoked_by_platform_user_id",
                table: "platform_user_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_roles_created_by_platform_user_id",
                table: "platform_roles");

            migrationBuilder.DropIndex(
                name: "IX_platform_roles_updated_by_platform_user_id",
                table: "platform_roles");

            migrationBuilder.DropIndex(
                name: "IX_platform_role_permissions_granted_by_platform_user_id",
                table: "platform_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_role_permissions_revoked_by_platform_user_id",
                table: "platform_role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_permissions_created_by_platform_user_id",
                table: "platform_permissions");

            migrationBuilder.DropIndex(
                name: "IX_platform_permissions_updated_by_platform_user_id",
                table: "platform_permissions");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "failed_login_count",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "job_title",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "platform_users");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "platform_user_roles");

            migrationBuilder.DropColumn(
                name: "assigned_by_platform_user_id",
                table: "platform_user_roles");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "platform_user_roles");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "platform_user_roles");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "platform_user_roles");

            migrationBuilder.DropColumn(
                name: "assigned_at",
                table: "platform_user_permissions");

            migrationBuilder.DropColumn(
                name: "assigned_by_platform_user_id",
                table: "platform_user_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "platform_user_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "platform_user_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "platform_user_permissions");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "platform_roles");

            migrationBuilder.DropColumn(
                name: "is_system_role",
                table: "platform_roles");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "platform_roles");

            migrationBuilder.DropColumn(
                name: "granted_at",
                table: "platform_role_permissions");

            migrationBuilder.DropColumn(
                name: "granted_by_platform_user_id",
                table: "platform_role_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "platform_role_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "platform_role_permissions");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "platform_role_permissions");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "platform_permissions");

            migrationBuilder.DropColumn(
                name: "module_key",
                table: "platform_permissions");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "platform_permissions");
        }
    }
}
