using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations.Oneverce
{
    /// <inheritdoc />
    public partial class SeedOneverceMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Seed.Oneverce.OneverceMediaAssetsSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Seed.Oneverce.OneverceMediaAssetsSeedData.DownSql);
        }
    }
}
