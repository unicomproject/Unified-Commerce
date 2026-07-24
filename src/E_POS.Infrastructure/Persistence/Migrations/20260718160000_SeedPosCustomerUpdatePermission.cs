using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds only <c>customers.update</c> (definition + development Cashier assignment).
/// Does not reseed the full POS permission catalogue.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260718160000_SeedPosCustomerUpdatePermission")]
public partial class SeedPosCustomerUpdatePermission : Migration
{
    /// <summary>
    /// Deterministic ID for customers.update. Must remain unique across permission_definitions.
    /// Do not reuse 77777777-0316 (owned by sales.checkout).
    /// </summary>
    public const string CustomersUpdatePermissionId =
        "77777777-0338-4000-8000-000000000001";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosCustomerUpdatePermissionSeedData.UpSql);
        migrationBuilder.Sql(DevelopmentPosCustomerUpdatePermissionSeedData.CashierAssignmentUpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosCustomerUpdatePermissionSeedData.CashierAssignmentDownSql);
        migrationBuilder.Sql(DevelopmentPosCustomerUpdatePermissionSeedData.DownSql);
    }
}
