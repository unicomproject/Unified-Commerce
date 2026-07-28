using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Enforces approved tenant lifecycle values on <c>tenants.status</c>.
/// Must run after <see cref="RepairTenantLifecycleStatusData"/>.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260727151000_AddTenantLifecycleStatusCheckConstraint")]
public partial class AddTenantLifecycleStatusCheckConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_tenants_status",
            table: "tenants",
            sql: "status IN ('draft', 'pending_payment', 'pending_activation', 'active', 'suspended', 'cancelled')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_tenants_status",
            table: "tenants");
    }
}
