using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260727154500_SeedDevelopmentCashierProfileImage")]
public sealed class SeedDevelopmentCashierProfileImage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentCashierProfileImageSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentCashierProfileImageSeedData.DownSql);
    }
}
