using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260724103000_SeedDevelopmentStorefrontCustomers")]
public partial class SeedDevelopmentStorefrontCustomers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentStorefrontCustomersSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentStorefrontCustomersSeedData.DownSql);
    }
}
