using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EPosDbContext))]
[Migration("20260902130000_BackfillDevelopmentMerchandiseProductImageUrls")]
public partial class BackfillDevelopmentMerchandiseProductImageUrls : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseCatalogSeedData.MerchandiseProductImageMediaAssetsUpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseCatalogSeedData.MerchandiseProductImageMediaAssetsDownSql);
    }
}
