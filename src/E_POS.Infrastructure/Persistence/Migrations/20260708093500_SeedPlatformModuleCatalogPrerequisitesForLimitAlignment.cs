using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260708093500_SeedPlatformModuleCatalogPrerequisitesForLimitAlignment")]
public partial class SeedPlatformModuleCatalogPrerequisitesForLimitAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PlatformModuleCatalogPrerequisiteSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PlatformModuleCatalogPrerequisiteSeedData.DownSql);
    }
}
