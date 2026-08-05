using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerExternalAuthAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_external_auth_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_auth_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    provider_subject = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    provider_email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    provider_email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_external_auth_accounts", x => x.id);
                    table.CheckConstraint("ck_customer_ext_auth_provider_code", "provider_code IN ('GOOGLE')");
                    table.CheckConstraint("ck_customer_ext_auth_provider_subject", "length(trim(provider_subject)) > 0");
                    table.CheckConstraint("ck_customer_ext_auth_status", "status IN ('ACTIVE', 'DISABLED', 'DELETED')");
                    table.ForeignKey(
                        name: "fk_customer_external_auth_accounts_auth_account",
                        columns: x => new { x.tenant_id, x.customer_auth_account_id },
                        principalTable: "customer_auth_accounts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_external_auth_accounts_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_customer_ext_auth_tenant_account_provider",
                table: "customer_external_auth_accounts",
                columns: new[] { "tenant_id", "customer_auth_account_id", "provider_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customer_ext_auth_tenant_id",
                table: "customer_external_auth_accounts",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customer_ext_auth_tenant_provider_subject",
                table: "customer_external_auth_accounts",
                columns: new[] { "tenant_id", "provider_code", "provider_subject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_external_auth_accounts");
        }
    }
}
