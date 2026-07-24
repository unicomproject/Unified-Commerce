using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds development tenant default return policy and assigns it to MER-* products
/// that have no <c>return_policy_id</c>. Data-only; no schema change.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260718180000_AssignReturnPolicyToExistingPosProducts")]
public partial class AssignReturnPolicyToExistingPosProducts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosReturnPolicySeedData.UpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentPosReturnPolicySeedData.DownSql);
    }
}
