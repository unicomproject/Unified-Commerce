using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260724090000_SeedDevelopmentMediaAssetsPhase4D")]
    public partial class SeedDevelopmentMediaAssetsPhase4D : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentMediaAssetsSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentMediaAssetsSeedData.DownSql);
        }
    }
}