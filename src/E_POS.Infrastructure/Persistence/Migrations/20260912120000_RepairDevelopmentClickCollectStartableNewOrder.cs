using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260912120000_RepairDevelopmentClickCollectStartableNewOrder")]
public sealed class RepairDevelopmentClickCollectStartableNewOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(DevelopmentClickCollectStartableNewOrderSeedData.UpSql);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Additive Development fixture repair; do not tear down runtime graphs.
    }
}
