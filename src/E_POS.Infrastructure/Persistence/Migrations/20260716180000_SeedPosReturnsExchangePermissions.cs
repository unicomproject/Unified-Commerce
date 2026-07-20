using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds returns.create, exchanges.view, exchanges.create, pos.refund.approve,
/// the pos.exchanges feature, Cashier grants (excluding approve), and
/// STORE_MANAGER / TENANT_ADMIN grants (including approve).
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260716180000_SeedPosReturnsExchangePermissions")]
public partial class SeedPosReturnsExchangePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosReturnsExchangePermissionsSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosReturnsExchangePermissionsSeedData.DownSql);
    }
}
