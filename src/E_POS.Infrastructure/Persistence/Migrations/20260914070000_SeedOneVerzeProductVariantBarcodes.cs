using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed.OneVerze;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EPosDbContext))]
[Migration("20260914070000_SeedOneVerzeProductVariantBarcodes")]
public partial class SeedOneVerzeProductVariantBarcodes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(OneVerzeProductVariantBarcodeSeedData.UpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(OneVerzeProductVariantBarcodeSeedData.DownSql);
    }
}
