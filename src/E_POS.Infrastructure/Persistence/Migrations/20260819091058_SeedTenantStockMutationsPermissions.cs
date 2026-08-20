using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTenantStockMutationsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SELECT 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM tenant_user_role_permissions WHERE permission_code IN ('tenant.stock.adjustments.create', 'tenant.stock.transfers.create');
                DELETE FROM permission_definitions WHERE permission_code IN ('tenant.stock.adjustments.create', 'tenant.stock.transfers.create');
            ");
        }
    }
}
