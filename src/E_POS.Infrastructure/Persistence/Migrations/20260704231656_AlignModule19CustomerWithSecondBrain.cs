using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule19CustomerWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_auth_accounts_customer_id_customers",
                table: "customer_auth_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts",
                table: "customer_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_consents_customer_id_customers",
                table: "customer_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_password_reset_tokens_customer_auth_account_id_customer_auth_accounts",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_password_reset_tokens_verified_otp_id_customer_verification_otps",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions",
                table: "customer_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_verification_otps_customer_id_customers",
                table: "customer_verification_otps");

            migrationBuilder.DropIndex(
                name: "IX_customer_verification_otps_customer_id",
                table: "customer_verification_otps");

            migrationBuilder.DropIndex(
                name: "IX_customer_refresh_tokens_customer_auth_session_id",
                table: "customer_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_password_reset_tokens_customer_auth_account_id",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_password_reset_tokens_verified_otp_id",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_consents_customer_id",
                table: "customer_consents");

            migrationBuilder.DropIndex(
                name: "IX_customer_auth_sessions_customer_auth_account_id",
                table: "customer_auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_customer_auth_accounts_customer_id",
                table: "customer_auth_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "customers",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "customer_verification_otps",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "customer_auth_sessions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_verification_otps_tenant_id_id",
                table: "customer_verification_otps",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_auth_sessions_tenant_id_id",
                table: "customer_auth_sessions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_auth_accounts_tenant_id_id",
                table: "customer_auth_accounts",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_customers_status",
                table: "customers",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'BLOCKED', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_customer_verification_otps_tenant_id_customer_id",
                table: "customer_verification_otps",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_verification_otps_tenant_id_id",
                table: "customer_verification_otps",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_verification_otps_status",
                table: "customer_verification_otps",
                sql: "status IN ('PENDING', 'VERIFIED', 'EXPIRED', 'INVALIDATED', 'FAILED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_verification_otps_verification_purpose",
                table: "customer_verification_otps",
                sql: "verification_purpose IN ('EMAIL_VERIFY', 'PHONE_VERIFY', 'PASSWORD_RESET', 'LOGIN_VERIFY')");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refresh_tokens_tenant_id_customer_auth_session_id",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "customer_auth_session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_password_reset_tokens_tenant_id_customer_auth_acco~",
                table: "customer_password_reset_tokens",
                columns: new[] { "tenant_id", "customer_auth_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_password_reset_tokens_tenant_id_verified_otp_id",
                table: "customer_password_reset_tokens",
                columns: new[] { "tenant_id", "verified_otp_id" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_password_reset_tokens_tenant_id_id",
                table: "customer_password_reset_tokens",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_consents_tenant_id_sales_channel_id",
                table: "customer_consents",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_consents_tenant_id_id",
                table: "customer_consents",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_consents_consent_type",
                table: "customer_consents",
                sql: "consent_type IN ('MARKETING_EMAIL', 'MARKETING_SMS', 'MARKETING_WHATSAPP', 'TERMS', 'PRIVACY')");

            migrationBuilder.CreateIndex(
                name: "IX_customer_auth_sessions_tenant_id_customer_auth_account_id",
                table: "customer_auth_sessions",
                columns: new[] { "tenant_id", "customer_auth_account_id" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_auth_sessions_tenant_id_id",
                table: "customer_auth_sessions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_auth_sessions_status",
                table: "customer_auth_sessions",
                sql: "status IN ('ACTIVE', 'EXPIRED', 'REVOKED')");

            migrationBuilder.CreateIndex(
                name: "uq_customer_auth_accounts_tenant_id_id",
                table: "customer_auth_accounts",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_auth_accounts_customer_id_customers",
                table: "customer_auth_accounts",
                columns: new[] { "tenant_id", "customer_id" },
                principalTable: "customers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_auth_accounts_tenant_id_tenants",
                table: "customer_auth_accounts",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts",
                table: "customer_auth_sessions",
                columns: new[] { "tenant_id", "customer_auth_account_id" },
                principalTable: "customer_auth_accounts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_auth_sessions_tenant_id_tenants",
                table: "customer_auth_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_consents_customer_id_customers",
                table: "customer_consents",
                columns: new[] { "tenant_id", "customer_id" },
                principalTable: "customers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_consents_sales_channel_id_sales_channels",
                table: "customer_consents",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_consents_tenant_id_tenants",
                table: "customer_consents",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_password_reset_tokens_customer_auth_account_id_customer_auth_accounts",
                table: "customer_password_reset_tokens",
                columns: new[] { "tenant_id", "customer_auth_account_id" },
                principalTable: "customer_auth_accounts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_password_reset_tokens_tenant_id_tenants",
                table: "customer_password_reset_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_password_reset_tokens_verified_otp_id_customer_verification_otps",
                table: "customer_password_reset_tokens",
                columns: new[] { "tenant_id", "verified_otp_id" },
                principalTable: "customer_verification_otps",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions",
                table: "customer_refresh_tokens",
                columns: new[] { "tenant_id", "customer_auth_session_id" },
                principalTable: "customer_auth_sessions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_refresh_tokens_tenant_id_tenants",
                table: "customer_refresh_tokens",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_verification_otps_customer_id_customers",
                table: "customer_verification_otps",
                columns: new[] { "tenant_id", "customer_id" },
                principalTable: "customers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_verification_otps_tenant_id_tenants",
                table: "customer_verification_otps",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_auth_accounts_customer_id_customers",
                table: "customer_auth_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_auth_accounts_tenant_id_tenants",
                table: "customer_auth_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts",
                table: "customer_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_auth_sessions_tenant_id_tenants",
                table: "customer_auth_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_consents_customer_id_customers",
                table: "customer_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_consents_sales_channel_id_sales_channels",
                table: "customer_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_consents_tenant_id_tenants",
                table: "customer_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_password_reset_tokens_customer_auth_account_id_customer_auth_accounts",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_password_reset_tokens_tenant_id_tenants",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_password_reset_tokens_verified_otp_id_customer_verification_otps",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions",
                table: "customer_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_refresh_tokens_tenant_id_tenants",
                table: "customer_refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_verification_otps_customer_id_customers",
                table: "customer_verification_otps");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_verification_otps_tenant_id_tenants",
                table: "customer_verification_otps");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customers_status",
                table: "customers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_verification_otps_tenant_id_id",
                table: "customer_verification_otps");

            migrationBuilder.DropIndex(
                name: "IX_customer_verification_otps_tenant_id_customer_id",
                table: "customer_verification_otps");

            migrationBuilder.DropIndex(
                name: "uq_customer_verification_otps_tenant_id_id",
                table: "customer_verification_otps");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_verification_otps_status",
                table: "customer_verification_otps");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_verification_otps_verification_purpose",
                table: "customer_verification_otps");

            migrationBuilder.DropIndex(
                name: "IX_customer_refresh_tokens_tenant_id_customer_auth_session_id",
                table: "customer_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_password_reset_tokens_tenant_id_customer_auth_acco~",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_password_reset_tokens_tenant_id_verified_otp_id",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "uq_customer_password_reset_tokens_tenant_id_id",
                table: "customer_password_reset_tokens");

            migrationBuilder.DropIndex(
                name: "IX_customer_consents_tenant_id_sales_channel_id",
                table: "customer_consents");

            migrationBuilder.DropIndex(
                name: "uq_customer_consents_tenant_id_id",
                table: "customer_consents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_consents_consent_type",
                table: "customer_consents");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_auth_sessions_tenant_id_id",
                table: "customer_auth_sessions");

            migrationBuilder.DropIndex(
                name: "IX_customer_auth_sessions_tenant_id_customer_auth_account_id",
                table: "customer_auth_sessions");

            migrationBuilder.DropIndex(
                name: "uq_customer_auth_sessions_tenant_id_id",
                table: "customer_auth_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_auth_sessions_status",
                table: "customer_auth_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_auth_accounts_tenant_id_id",
                table: "customer_auth_accounts");

            migrationBuilder.DropIndex(
                name: "uq_customer_auth_accounts_tenant_id_id",
                table: "customer_auth_accounts");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "customers",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "customer_verification_otps",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "customer_auth_sessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.CreateIndex(
                name: "IX_customer_verification_otps_customer_id",
                table: "customer_verification_otps",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refresh_tokens_customer_auth_session_id",
                table: "customer_refresh_tokens",
                column: "customer_auth_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_password_reset_tokens_customer_auth_account_id",
                table: "customer_password_reset_tokens",
                column: "customer_auth_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_password_reset_tokens_verified_otp_id",
                table: "customer_password_reset_tokens",
                column: "verified_otp_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_consents_customer_id",
                table: "customer_consents",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_auth_sessions_customer_auth_account_id",
                table: "customer_auth_sessions",
                column: "customer_auth_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_auth_accounts_customer_id",
                table: "customer_auth_accounts",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_auth_accounts_customer_id_customers",
                table: "customer_auth_accounts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_auth_sessions_customer_auth_account_id_customer_auth_accounts",
                table: "customer_auth_sessions",
                column: "customer_auth_account_id",
                principalTable: "customer_auth_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_consents_customer_id_customers",
                table: "customer_consents",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_password_reset_tokens_customer_auth_account_id_customer_auth_accounts",
                table: "customer_password_reset_tokens",
                column: "customer_auth_account_id",
                principalTable: "customer_auth_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_password_reset_tokens_verified_otp_id_customer_verification_otps",
                table: "customer_password_reset_tokens",
                column: "verified_otp_id",
                principalTable: "customer_verification_otps",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_refresh_tokens_customer_auth_session_id_customer_auth_sessions",
                table: "customer_refresh_tokens",
                column: "customer_auth_session_id",
                principalTable: "customer_auth_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_verification_otps_customer_id_customers",
                table: "customer_verification_otps",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
