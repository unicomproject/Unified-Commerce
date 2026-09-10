using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260908180000_RepairDevelopmentClickCollectBarcodeSnapshots")]
public sealed class RepairDevelopmentClickCollectBarcodeSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentClickCollectBarcodeSnapshotRepairSeedData.RepairSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentional no-op. Snapshot repair must not erase order-time barcode truth.
    }
}
