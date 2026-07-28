using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Ensures the stable development tenant admin has current-schema role,
/// permission, and feature entitlement data.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260727130000_EnsureDevelopmentTenantAdminAccess")]
public partial class EnsureDevelopmentTenantAdminAccess : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentTenantAdminAccessSeedData.UpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentTenantAdminAccessSeedData.DownSql);
    }
}
