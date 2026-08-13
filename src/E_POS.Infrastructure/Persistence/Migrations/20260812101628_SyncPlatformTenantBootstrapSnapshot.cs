using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Snapshot-only sync for bootstrap import/idempotency entities.
/// Tables are created by <see cref="AddPlatformTenantBootstrapImportTables"/>.
/// </summary>
public partial class SyncPlatformTenantBootstrapSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
