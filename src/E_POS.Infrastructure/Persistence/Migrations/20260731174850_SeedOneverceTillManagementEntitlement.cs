using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedOneverceTillManagementEntitlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO tenant_feature_entitlements (
                    id, tenant_id, platform_feature_id, entitlement_status, source_type,
                    is_enabled, effective_from, created_at, updated_at
                )
                VALUES (
                    '35e165a2-0994-486d-9615-5645db1e2dc6',
                    '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                    '72500000-0000-0000-0000-000000000003',
                    'ENABLED',
                    'MANUAL',
                    TRUE,
                    now(),
                    now(),
                    now()
                )
                ON CONFLICT (tenant_id, platform_feature_id) DO UPDATE
                SET is_enabled = TRUE,
                    entitlement_status = 'ENABLED',
                    updated_at = now();
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_feature_entitlements
                WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
                  AND platform_feature_id = '72500000-0000-0000-0000-000000000003';
            """);
        }
    }
}
