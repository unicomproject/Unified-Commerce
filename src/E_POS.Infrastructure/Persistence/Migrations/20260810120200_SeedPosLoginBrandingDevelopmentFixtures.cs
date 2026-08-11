using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Restores deterministic Local Development fixtures required to exercise the
/// POS login branding contract. This migration is data-only.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260810120200_SeedPosLoginBrandingDevelopmentFixtures")]
public sealed class SeedPosLoginBrandingDevelopmentFixtures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(DevelopmentPosLoginBrandingSeedData.UpSql);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(DevelopmentPosLoginBrandingSeedData.DownSql);
}
