using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenPlatformAdminAuthNotNullConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_password_reset_tokens_expires_after_requested",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_login_audits_login_status",
                table: "platform_login_audits");

            // Idempotent backfill: re-apply Phase 8A derivations, then Phase 8G-b password-reset expiry.
            migrationBuilder.Sql("""
                UPDATE platform_refresh_tokens prt
                SET platform_user_id = pas.platform_user_id
                FROM platform_auth_sessions pas
                WHERE prt.platform_auth_session_id = pas.id
                  AND prt.platform_user_id IS NULL
                  AND pas.platform_user_id IS NOT NULL;

                UPDATE platform_refresh_tokens
                SET token_family_id = id
                WHERE token_family_id IS NULL;

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

                UPDATE platform_password_reset_tokens
                SET requested_at = created_at
                WHERE requested_at IS NULL;

                UPDATE platform_password_reset_tokens
                SET expires_at = COALESCE(requested_at, created_at) + INTERVAL '1 hour'
                WHERE expires_at IS NULL;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM platform_auth_sessions WHERE platform_user_id IS NULL) THEN
                        RAISE EXCEPTION 'platform_auth_sessions.platform_user_id nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_refresh_tokens WHERE platform_user_id IS NULL) THEN
                        RAISE EXCEPTION 'platform_refresh_tokens.platform_user_id nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_refresh_tokens WHERE token_family_id IS NULL) THEN
                        RAISE EXCEPTION 'platform_refresh_tokens.token_family_id nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_login_audits WHERE login_status IS NULL OR login_status = '') THEN
                        RAISE EXCEPTION 'platform_login_audits.login_status nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_login_audits WHERE attempted_at IS NULL) THEN
                        RAISE EXCEPTION 'platform_login_audits.attempted_at nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_login_audits WHERE authentication_method IS NULL OR authentication_method = '') THEN
                        RAISE EXCEPTION 'platform_login_audits.authentication_method nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_password_reset_tokens WHERE requested_at IS NULL) THEN
                        RAISE EXCEPTION 'platform_password_reset_tokens.requested_at nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (SELECT 1 FROM platform_password_reset_tokens WHERE expires_at IS NULL) THEN
                        RAISE EXCEPTION 'platform_password_reset_tokens.expires_at nulls remain. Run backfill before NOT NULL hardening.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM platform_password_reset_tokens
                        WHERE expires_at IS NOT NULL
                          AND requested_at IS NOT NULL
                          AND expires_at <= requested_at
                    ) THEN
                        RAISE EXCEPTION 'platform_password_reset_tokens expires_at must be greater than requested_at before hardening.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "token_family_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "requested_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "login_status",
                table: "platform_login_audits",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authentication_method",
                table: "platform_login_audits",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "attempted_at",
                table: "platform_login_audits",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "platform_auth_sessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_password_reset_tokens_expires_after_requested",
                table: "platform_password_reset_tokens",
                sql: "expires_at > requested_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_login_audits_login_status",
                table: "platform_login_audits",
                sql: "login_status IN ('SUCCESS', 'FAILED', 'LOCKED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_password_reset_tokens_expires_after_requested",
                table: "platform_password_reset_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_login_audits_login_status",
                table: "platform_login_audits");

            migrationBuilder.AlterColumn<Guid>(
                name: "token_family_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "platform_refresh_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "requested_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                table: "platform_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "login_status",
                table: "platform_login_audits",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "authentication_method",
                table: "platform_login_audits",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "attempted_at",
                table: "platform_login_audits",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "platform_user_id",
                table: "platform_auth_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_password_reset_tokens_expires_after_requested",
                table: "platform_password_reset_tokens",
                sql: "expires_at IS NULL OR requested_at IS NULL OR expires_at > requested_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_login_audits_login_status",
                table: "platform_login_audits",
                sql: "login_status IS NULL OR login_status IN ('SUCCESS', 'FAILED', 'LOCKED')");
        }
    }
}
