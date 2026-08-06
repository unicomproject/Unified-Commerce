using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs only the recognisable legacy development Retail lookup after the
/// business-type column rename. The migration intentionally uses the seed's
/// natural provenance rather than its development-only identifier.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260804190000_BackfillDevelopmentRetailBusinessCode")]
public sealed class BackfillDevelopmentRetailBusinessCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(RetailBusinessCodeRepairSql.Up);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data provenance cannot be reconstructed during rollback. Leaving the
        // valid code in place is deliberately safer than clearing a value that
        // may have since become authoritative.
    }
}
