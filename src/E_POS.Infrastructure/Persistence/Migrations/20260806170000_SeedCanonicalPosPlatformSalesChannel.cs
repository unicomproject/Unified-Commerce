using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using E_POS.Infrastructure.Persistence.Seed;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260806170000_SeedCanonicalPosPlatformSalesChannel")]
public sealed class SeedCanonicalPosPlatformSalesChannel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_platform_sales_channels_channel_type",
            table: "platform_sales_channels");

        migrationBuilder.AddCheckConstraint(
            name: "ck_platform_sales_channels_channel_type",
            table: "platform_sales_channels",
            sql: "channel_type IN ('PHYSICAL', 'ONLINE', 'POS', 'AGGREGATOR', 'B2B', 'OTHER')");

        migrationBuilder.Sql(PlatformSalesChannelSeedData.CanonicalPosUpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive because tenant sales channels may reference
        // the canonical POS definition after this migration has been applied.
    }
}
