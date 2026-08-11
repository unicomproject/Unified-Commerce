using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Registers the target Development POS login visual assets through the
/// canonical media and tenant-setting infrastructure. Data-only migration.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260810120400_SeedTargetPosLoginBrandingAssets")]
public sealed class SeedTargetPosLoginBrandingAssets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(DevelopmentPosLoginBrandingTargetSeedData.UpSql);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(DevelopmentPosLoginBrandingTargetSeedData.DownSql);
}
