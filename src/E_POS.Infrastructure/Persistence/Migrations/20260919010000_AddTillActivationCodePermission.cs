using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260919010000_AddTillActivationCodePermission")]
public sealed class AddTillActivationCodePermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(TillActivationPermissionSeedData.UpSql);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve security configuration and explicit role decisions on rollback.
    }
}
