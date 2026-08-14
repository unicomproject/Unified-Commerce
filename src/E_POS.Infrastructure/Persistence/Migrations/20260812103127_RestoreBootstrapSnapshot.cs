using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Snapshot-only restore for bootstrap entities including request_hash on idempotency records.
/// Physical tables are created by <see cref="AddPlatformTenantBootstrapImportTables"/>.
/// </summary>
public partial class RestoreBootstrapSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
