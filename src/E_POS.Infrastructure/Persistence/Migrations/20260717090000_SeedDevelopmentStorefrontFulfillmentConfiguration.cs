using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EPosDbContext))]
[Migration("20260717090000_SeedDevelopmentStorefrontFulfillmentConfiguration")]
public partial class SeedDevelopmentStorefrontFulfillmentConfiguration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentStorefrontFulfillmentSeedData.UpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentStorefrontFulfillmentSeedData.DownSql);
    }
}
