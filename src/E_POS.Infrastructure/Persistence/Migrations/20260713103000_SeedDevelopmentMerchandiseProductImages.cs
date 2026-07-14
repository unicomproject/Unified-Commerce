using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EPosDbContext))]
[Migration("20260713103000_SeedDevelopmentMerchandiseProductImages")]
public partial class SeedDevelopmentMerchandiseProductImages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseCatalogSeedData.ProductImageUpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseCatalogSeedData.ProductImageDownSql);
    }
}
