using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Ensures the stable development tenant has a minimal database-backed return
/// reason catalog. Existing tenant rows, including inactive or customized rows,
/// are left unchanged.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260718190000_EnsureDevelopmentTenantReturnReasons")]
public partial class EnsureDevelopmentTenantReturnReasons : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentReturnReasonsSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentReturnReasonsSeedData.DownSql);
    }
}
