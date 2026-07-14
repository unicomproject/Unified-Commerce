using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EPosDbContext))]
[Migration("20260710143000_SeedDevelopmentVariableProductCatalog")]
public partial class SeedDevelopmentVariableProductCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentVariableProductCatalogSeedData.UpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentVariableProductCatalogSeedData.DownSql);
    }
}
