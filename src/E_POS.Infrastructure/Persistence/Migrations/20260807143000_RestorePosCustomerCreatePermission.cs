using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Restores the canonical POS customers.create definition and its approved
/// development Cashier assignment after the historical removal migration.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260807143000_RestorePosCustomerCreatePermission")]
public partial class RestorePosCustomerCreatePermission : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosCustomerCreatePermissionSeedData.UpSql);
        migrationBuilder.Sql(
            DevelopmentPosCustomerCreatePermissionSeedData.CashierAssignmentUpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            DevelopmentPosCustomerCreatePermissionSeedData.CashierAssignmentDownSql);
        migrationBuilder.Sql(DevelopmentPosCustomerCreatePermissionSeedData.DownSql);
    }
}
