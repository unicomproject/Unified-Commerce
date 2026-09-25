using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260910120000_ProvisionWorkspacePermissions")]
public sealed class ProvisionWorkspacePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(WorkspacePermissionSeedData.UpSql);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Additive access data may have acquired assignments after deployment.
        // Do not destructively remove grants on rollback.
    }
}
