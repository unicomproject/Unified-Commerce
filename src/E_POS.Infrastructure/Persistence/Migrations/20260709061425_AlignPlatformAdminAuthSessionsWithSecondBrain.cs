using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPlatformAdminAuthSessionsWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_platform_auth_sessions_platform_user_id",
                table: "platform_auth_sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "platform_user_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaced_by_token_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoke_reason",
                table: "platform_refresh_tokens",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "token_family_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "platform_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "attempted_at",
                table: "platform_login_audits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "authentication_method",
                table: "platform_login_audits",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                table: "platform_login_audits",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "platform_login_audits",
                type: "varchar(45)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "login_status",
                table: "platform_login_audits",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "platform_auth_session_id",
                table: "platform_login_audits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "risk_score",
                table: "platform_login_audits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "platform_login_audits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                table: "platform_auth_sessions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "platform_auth_sessions",
                type: "varchar(45)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_seen_at",
                table: "platform_auth_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoke_reason",
                table: "platform_auth_sessions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "platform_auth_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "platform_auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "platform_auth_sessions",
                type: "text",
                nullable: true);

            // Idempotent backfill: derive SB columns from existing status/timestamps only.
            // Does not invent IP, user-agent, device, risk score, session/token hashes, or user IDs.
            migrationBuilder.Sql("""
                UPDATE platform_auth_sessions
                SET revoked_at = COALESCE(updated_at, created_at)
                WHERE status = 'REVOKED'
                  AND revoked_at IS NULL;

                UPDATE platform_refresh_tokens prt
                SET platform_user_id = pas.platform_user_id
                FROM platform_auth_sessions pas
                WHERE prt.platform_auth_session_id = pas.id
                  AND prt.platform_user_id IS NULL
                  AND pas.platform_user_id IS NOT NULL;

                UPDATE platform_refresh_tokens
                SET token_family_id = id
                WHERE token_family_id IS NULL;

                UPDATE platform_refresh_tokens
                SET used_at = COALESCE(updated_at, created_at)
                WHERE status = 'USED'
                  AND used_at IS NULL;

                UPDATE platform_refresh_tokens
                SET revoked_at = COALESCE(updated_at, created_at)
                WHERE status IN ('REVOKED', 'EXPIRED')
                  AND revoked_at IS NULL;

                UPDATE platform_password_reset_tokens
                SET requested_at = created_at
                WHERE requested_at IS NULL;

                UPDATE platform_password_reset_tokens
                SET used_at = COALESCE(updated_at, created_at)
                WHERE status = 'USED'
                  AND used_at IS NULL;

                UPDATE platform_password_reset_tokens
                SET revoked_at = COALESCE(updated_at, created_at)
                WHERE status = 'REVOKED'
                  AND revoked_at IS NULL;

                UPDATE platform_login_audits
                SET attempted_at = created_at
                WHERE attempted_at IS NULL;

                UPDATE platform_login_audits
                SET login_status = login_result
                WHERE (login_status IS NULL OR login_status = '')
                  AND login_result IS NOT NULL
                  AND login_result <> '';

                UPDATE platform_login_audits
                SET authentication_method = 'PASSWORD'
                WHERE authentication_method IS NULL
                   OR authentication_method = '';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_platform_refresh_tokens_platform_user_id_status",
                table: "platform_refresh_tokens",
                columns: new[] { "platform_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_refresh_tokens_replaced_by_token_id",
                table: "platform_refresh_tokens",
                column: "replaced_by_token_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_refresh_tokens_revoked_by_platform_user_id",
                table: "platform_refresh_tokens",
                column: "revoked_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_refresh_tokens_token_family_id",
                table: "platform_refresh_tokens",
                column: "token_family_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_password_reset_tokens_expires_after_requested",
                table: "platform_password_reset_tokens",
                sql: "expires_at IS NULL OR requested_at IS NULL OR expires_at > requested_at");

            migrationBuilder.CreateIndex(
                name: "ix_platform_login_audits_attempted_at",
                table: "platform_login_audits",
                column: "attempted_at");

            migrationBuilder.CreateIndex(
                name: "ix_platform_login_audits_platform_auth_session_id",
                table: "platform_login_audits",
                column: "platform_auth_session_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_login_audits_login_status",
                table: "platform_login_audits",
                sql: "login_status IS NULL OR login_status IN ('SUCCESS', 'FAILED', 'LOCKED')");

            migrationBuilder.CreateIndex(
                name: "ix_platform_auth_sessions_platform_user_id_revoked_at",
                table: "platform_auth_sessions",
                columns: new[] { "platform_user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_auth_sessions_revoked_by_platform_user_id",
                table: "platform_auth_sessions",
                column: "revoked_by_platform_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_platform_auth_sessions_revoked_by_platform_user_id_platform_users",
                table: "platform_auth_sessions",
                column: "revoked_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_login_audits_platform_auth_session_id_platform_auth_sessions",
                table: "platform_login_audits",
                column: "platform_auth_session_id",
                principalTable: "platform_auth_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_refresh_tokens_platform_user_id_platform_users",
                table: "platform_refresh_tokens",
                column: "platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_refresh_tokens_replaced_by_token_id_platform_refresh_tokens",
                table: "platform_refresh_tokens",
                column: "replaced_by_token_id",
                principalTable: "platform_refresh_tokens",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_platform_refresh_tokens_revoked_by_platform_user_id_platform_users",
                table: "platform_refresh_tokens",
                column: "revoked_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_platform_auth_sessions_revoked_by_platform_user_id_platform_users",
                table: "platform_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_login_audits_platform_auth_session_id_platform_auth_sessions",
                table: "platform_login_audits");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_refresh_tokens_platform_user_id_platform_users",
                table: "platform_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_refresh_tokens_replaced_by_token_id_platform_refresh_tokens",
                table: "platform_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_platform_refresh_tokens_revoked_by_platform_user_id_platform_users",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_platform_refresh_tokens_platform_user_id_status",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_platform_refresh_tokens_replaced_by_token_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_platform_refresh_tokens_revoked_by_platform_user_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_platform_refresh_tokens_token_family_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_password_reset_tokens_expires_after_requested",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "ix_platform_login_audits_attempted_at",
                table: "platform_login_audits");

            migrationBuilder.DropIndex(
                name: "ix_platform_login_audits_platform_auth_session_id",
                table: "platform_login_audits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_login_audits_login_status",
                table: "platform_login_audits");

            migrationBuilder.DropIndex(
                name: "ix_platform_auth_sessions_platform_user_id_revoked_at",
                table: "platform_auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_platform_auth_sessions_revoked_by_platform_user_id",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "platform_user_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replaced_by_token_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoke_reason",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "token_family_id",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "platform_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "attempted_at",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "authentication_method",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "login_status",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "platform_auth_session_id",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "risk_score",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "platform_login_audits");

            migrationBuilder.DropColumn(
                name: "device_name",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoke_reason",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "platform_auth_sessions");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "platform_auth_sessions");

            migrationBuilder.CreateIndex(
                name: "IX_platform_auth_sessions_platform_user_id",
                table: "platform_auth_sessions",
                column: "platform_user_id");
        }
    }
}
