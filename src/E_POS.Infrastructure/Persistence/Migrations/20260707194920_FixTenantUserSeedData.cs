using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantUserSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE tenant_users 
                SET account_status = 'ACTIVE',
                    encrypted_password = 'PBKDF2-SHA256:100000:zG7O+AY1EJBG5+sCXDBinA==:weI+nABmBRNW19gQODOHn5D2q8SUQ0rVJy0NITO/Qyo=',
                    updated_at = now()
                WHERE email IN (
                    'TENANTADMIN001@GMAIL.COM',
                    'STOREMANAGER001@GMAIL.COM',
                    'CASHIER001@GMAIL.COM',
                    'INVENTORY001@GMAIL.COM',
                    'FULFILLMENT001@GMAIL.COM'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE tenant_users 
                SET account_status = '',
                    encrypted_password = '',
                    updated_at = now()
                WHERE email IN (
                    'TENANTADMIN001@GMAIL.COM',
                    'STOREMANAGER001@GMAIL.COM',
                    'CASHIER001@GMAIL.COM',
                    'INVENTORY001@GMAIL.COM',
                    'FULFILLMENT001@GMAIL.COM'
                );
                """);
        }
    }
}
