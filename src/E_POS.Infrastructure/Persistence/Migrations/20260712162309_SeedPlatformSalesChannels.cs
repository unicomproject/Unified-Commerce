using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlatformSalesChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.PlatformSalesChannelSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.PlatformSalesChannelSeedData.DownSql);
        }
    }
}
