using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Migration-history-safe replay of the guarded Retail repair for databases
/// that may already have recorded the original 20260804190000 migration.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260805120000_ApplyProductionSafeRetailBusinessCodeRepair")]
public sealed class ApplyProductionSafeRetailBusinessCodeRepair : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(RetailBusinessCodeRepairSql.Up);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive. See the disposition decision record.
    }
}
