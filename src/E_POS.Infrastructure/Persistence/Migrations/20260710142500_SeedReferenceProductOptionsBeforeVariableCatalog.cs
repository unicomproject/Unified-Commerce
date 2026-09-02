using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260710142500_SeedReferenceProductOptionsBeforeVariableCatalog")]
public sealed class SeedReferenceProductOptionsBeforeVariableCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ReferenceProductOptionTemplateSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
