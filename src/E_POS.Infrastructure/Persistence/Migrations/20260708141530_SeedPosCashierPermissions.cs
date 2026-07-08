using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPosCashierPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosPermissionCatalogSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosNewSalePermissionsSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosPaymentReceiptPermissionsSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosCashierPermissionAssignmentSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosCashierPermissionAssignmentSeedData.DownSql);
            migrationBuilder.Sql(DevelopmentPosPaymentReceiptPermissionsSeedData.DownSql);
            migrationBuilder.Sql(DevelopmentPosNewSalePermissionsSeedData.DownSql);
            migrationBuilder.Sql(DevelopmentPosPermissionCatalogSeedData.DownSql);
        }
    }
}
