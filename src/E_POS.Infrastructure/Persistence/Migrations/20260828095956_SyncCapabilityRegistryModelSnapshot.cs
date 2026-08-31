using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncCapabilityRegistryModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Model snapshot synchronization only.
            // DDL operations, column creation, backfills, FKs, and check constraints
            // were applied in 20260828150000_AddExplicitScopeToCapabilityRegistry and
            // 20260828160000_LinkPlatformPermissionsToCapabilityRegistry.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
