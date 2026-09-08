using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Chunk 3: seed role-assignable Cashier POS canonical permission definitions from
/// CashierPosCanonicalPermissionCatalog and backfill parent→child grants for
/// existing tenant role/user assignments. Additive / idempotent. No schema change.
/// Runtime authorization and Flutter visibility are deferred.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260904140000_SeedCashierPosChunk3CanonicalPermissions")]
public sealed class SeedCashierPosChunk3CanonicalPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CashierPosChunk3PermissionSeedData.DefinitionUpsertSql);
        migrationBuilder.Sql(CashierPosChunk3PermissionSeedData.CompatibilityBackfillSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CashierPosChunk3PermissionSeedData.FineGrainedAssignmentDownSql);
        migrationBuilder.Sql(CashierPosChunk3PermissionSeedData.FineGrainedDefinitionDownSql);
    }
}
