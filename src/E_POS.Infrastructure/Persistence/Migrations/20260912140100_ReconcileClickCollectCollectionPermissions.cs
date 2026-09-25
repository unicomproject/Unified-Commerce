using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Upserts Chunk 3 catalog definitions (including new collection.* codes) and
/// backfills grants from legacy pos.online_orders.collection.* seeds.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260912140100_ReconcileClickCollectCollectionPermissions")]
public sealed class ReconcileClickCollectCollectionPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(OnlineOrderCanonicalPermissionReconciliationSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Definitions remain in catalog; grant backfill cleanup uses shared notes.
        migrationBuilder.Sql(OnlineOrderCanonicalPermissionReconciliationSeedData.DownSql);
    }
}
