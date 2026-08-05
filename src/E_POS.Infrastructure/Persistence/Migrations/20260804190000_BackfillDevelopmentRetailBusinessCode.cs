using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs the development Retail lookup after the business-type column rename.
/// Flow 4 requires a non-empty stable code for create-options and tenant finalization.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260804190000_BackfillDevelopmentRetailBusinessCode")]
public sealed class BackfillDevelopmentRetailBusinessCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE business_types
            SET business_code = 'RETAIL',
                updated_at = now()
            WHERE id = '44444444-0002-4000-8000-000000000001'
              AND COALESCE(business_code, '') = '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE business_types
            SET business_code = '',
                updated_at = now()
            WHERE id = '44444444-0002-4000-8000-000000000001'
              AND business_code = 'RETAIL';
            """);
    }
}
