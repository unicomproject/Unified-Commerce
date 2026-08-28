using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260824120000_SeedPosOnlineOrderPermissions")]
public sealed class SeedPosOnlineOrderPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.UpSql);
        migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.CashierAssignmentUpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.CashierAssignmentDownSql);
        migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.DownSql);
    }
}
