using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds pos.hardware.settings and grants it to the development Cashier role.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260728160000_SeedPosHardwareSettingsPermission")]
public sealed class SeedPosHardwareSettingsPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            DevelopmentPosHardwareSettingsPermissionSeedData.UpSql);
        // migrationBuilder.Sql(
        //     DevelopmentPosHardwareSettingsPermissionSeedData
        //         .CashierAssignmentUpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            DevelopmentPosHardwareSettingsPermissionSeedData
                .CashierAssignmentDownSql);
        migrationBuilder.Sql(
            DevelopmentPosHardwareSettingsPermissionSeedData.DownSql);
    }
}

