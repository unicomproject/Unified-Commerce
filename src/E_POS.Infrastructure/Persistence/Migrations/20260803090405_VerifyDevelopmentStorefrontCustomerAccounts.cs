using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VerifyDevelopmentStorefrontCustomerAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE customer_auth_accounts account
                SET email_verified_at = COALESCE(account.email_verified_at, now()),
                    failed_login_count = 0,
                    locked_until = NULL,
                    status = 'ACTIVE',
                    updated_at = now()
                FROM customers customer
                WHERE account.tenant_id = customer.tenant_id
                  AND account.customer_id = customer.id
                  AND customer.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
                  AND customer.normalized_email IN (
                      'CUSTOMER1@EXAMPLE.COM',
                      'CUSTOMER2@EXAMPLE.COM',
                      'CUSTOMER3@EXAMPLE.COM'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
