using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule19CustomerWithSecondBrainColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                table: "customers",
                newName: "display_name");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_phone",
                table: "customers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "customers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "customers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "anonymized_at",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "customers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "customers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "customers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "customers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_sales_channel_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "customers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "verification_purpose",
                table: "customer_verification_otps",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_recipient_value",
                table: "customer_verification_otps",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "max_attempts",
                table: "customer_verification_otps",
                type: "integer",
                nullable: false,
                defaultValue: 5,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "attempt_count",
                table: "customer_verification_otps",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "delivery_channel",
                table: "customer_verification_otps",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "customer_verification_otps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "invalidated_at",
                table: "customer_verification_otps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_sent_at",
                table: "customer_verification_otps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "otp_hash",
                table: "customer_verification_otps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "customer_verification_otps",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "customer_verification_otps",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_value",
                table: "customer_verification_otps",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<IPAddress>(
                name: "request_ip_address",
                table: "customer_verification_otps",
                type: "inet",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_user_agent",
                table: "customer_verification_otps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "resend_count",
                table: "customer_verification_otps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sent_at",
                table: "customer_verification_otps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_at",
                table: "customer_verification_otps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "customer_refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "replaced_by_token_id",
                table: "customer_refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "customer_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "customer_refresh_tokens",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "customer_refresh_tokens",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "token_family_id",
                table: "customer_refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "customer_refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "customer_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<IPAddress>(
                name: "request_ip_address",
                table: "customer_password_reset_tokens",
                type: "inet",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_user_agent",
                table: "customer_password_reset_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "customer_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "customer_password_reset_tokens",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "customer_password_reset_tokens",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "used_at",
                table: "customer_password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_channel_id",
                table: "customer_consents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_consents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "consent_source",
                table: "customer_consents",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "consent_status",
                table: "customer_consents",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<IPAddress>(
                name: "ip_address",
                table: "customer_consents",
                type: "inet",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "policy_version",
                table: "customer_consents",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "recorded_at",
                table: "customer_consents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "customer_consents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "withdrawn_at",
                table: "customer_consents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                table: "customer_auth_sessions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                table: "customer_auth_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<IPAddress>(
                name: "ip_address",
                table: "customer_auth_sessions",
                type: "inet",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_activity_at",
                table: "customer_auth_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "customer_auth_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "customer_auth_sessions",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "customer_auth_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "failed_login_count",
                table: "customer_auth_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_auth_accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_verified_at",
                table: "customer_auth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_failed_login_at",
                table: "customer_auth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at",
                table: "customer_auth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_password_changed_at",
                table: "customer_auth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                table: "customer_auth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "customer_auth_accounts",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "phone_verified_at",
                table: "customer_auth_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "customer_auth_accounts",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_refresh_tokens_tenant_id_id",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_source_sales_channel_id",
                table: "customers",
                column: "source_sales_channel_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customers_source_type",
                table: "customers",
                sql: "source_type IN ('POS', 'ECOMMERCE', 'CLICK_AND_COLLECT', 'IMPORT', 'MANUAL')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_verification_otps_delivery_channel",
                table: "customer_verification_otps",
                sql: "delivery_channel IN ('EMAIL', 'SMS', 'WHATSAPP')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_verification_otps_expires_at",
                table: "customer_verification_otps",
                sql: "expires_at > sent_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_verification_otps_resend_count",
                table: "customer_verification_otps",
                sql: "resend_count >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refresh_tokens_tenant_id_replaced_by_token_id",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "replaced_by_token_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_refresh_tokens_status",
                table: "customer_refresh_tokens",
                sql: "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_password_reset_tokens_status",
                table: "customer_password_reset_tokens",
                sql: "status IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_consents_consent_source",
                table: "customer_consents",
                sql: "consent_source IN ('POS', 'ECOMMERCE', 'CLICK_AND_COLLECT', 'ADMIN', 'IMPORT')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_consents_consent_status",
                table: "customer_consents",
                sql: "consent_status IN ('GRANTED', 'WITHDRAWN', 'EXPIRED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_auth_accounts_status",
                table: "customer_auth_accounts",
                sql: "status IN ('ACTIVE', 'LOCKED', 'DISABLED', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_refresh_tokens_replaced_by_token_id_customer_refresh_tokens",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "replaced_by_token_id" },
                principalTable: "customer_refresh_tokens",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_source_sales_channel_id_sales_channels",
                table: "customers",
                column: "source_sales_channel_id",
                principalTable: "sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_refresh_tokens_replaced_by_token_id_customer_refresh_tokens",
                table: "customer_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_source_sales_channel_id_sales_channels",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_source_sales_channel_id",
                table: "customers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customers_source_type",
                table: "customers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_verification_otps_delivery_channel",
                table: "customer_verification_otps");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_verification_otps_expires_at",
                table: "customer_verification_otps");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_verification_otps_resend_count",
                table: "customer_verification_otps");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_refresh_tokens_tenant_id_id",
                table: "customer_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_refresh_tokens_tenant_id_replaced_by_token_id",
                table: "customer_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_refresh_tokens_status",
                table: "customer_refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_password_reset_tokens_status",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_consents_consent_source",
                table: "customer_consents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_consents_consent_status",
                table: "customer_consents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_auth_accounts_status",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "anonymized_at",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "source_sales_channel_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "delivery_channel",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "invalidated_at",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "last_sent_at",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "otp_hash",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "recipient_value",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "request_ip_address",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "request_user_agent",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "resend_count",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "customer_verification_otps");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replaced_by_token_id",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "status",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "token_family_id",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "customer_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "request_ip_address",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "request_user_agent",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "status",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "used_at",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "consent_source",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "consent_status",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "policy_version",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "recorded_at",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "withdrawn_at",
                table: "customer_consents");

            migrationBuilder.DropColumn(
                name: "device_name",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "last_activity_at",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "customer_auth_sessions");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "last_failed_login_at",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "last_password_changed_at",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "phone_verified_at",
                table: "customer_auth_accounts");

            migrationBuilder.DropColumn(
                name: "status",
                table: "customer_auth_accounts");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "customers",
                newName: "name");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_phone",
                table: "customers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "customers",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "customers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "verification_purpose",
                table: "customer_verification_otps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_recipient_value",
                table: "customer_verification_otps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<int>(
                name: "max_attempts",
                table: "customer_verification_otps",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 5);

            migrationBuilder.AlterColumn<int>(
                name: "attempt_count",
                table: "customer_verification_otps",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "sales_channel_id",
                table: "customer_consents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_consents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "failed_login_count",
                table: "customer_auth_accounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "customer_auth_accounts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
