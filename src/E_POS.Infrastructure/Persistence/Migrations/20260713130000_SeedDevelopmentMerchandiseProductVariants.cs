using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260713130000_SeedDevelopmentMerchandiseProductVariants")]
public partial class SeedDevelopmentMerchandiseProductVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseProductVariantsSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseProductVariantsSeedData.DownSql);
    }
}
