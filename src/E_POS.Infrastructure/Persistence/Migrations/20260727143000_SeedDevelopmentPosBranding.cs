using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260727143000_SeedDevelopmentPosBranding")]
public partial class SeedDevelopmentPosBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosBrandingSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosBrandingSeedData.DownSql);
    }
}
