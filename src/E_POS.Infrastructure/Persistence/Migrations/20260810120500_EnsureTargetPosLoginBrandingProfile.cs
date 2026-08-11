using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Ensures the Development tenant has the canonical tenant profile required
/// to resolve its brand name and TENANT_LOGO through the public branding API.
/// Data-only follow-up for databases where the profile did not yet exist.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260810120500_EnsureTargetPosLoginBrandingProfile")]
public sealed class EnsureTargetPosLoginBrandingProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            INSERT INTO tenant_profiles (
                id, tenant_id, legal_name, trading_name, logo_media_asset_id,
                created_by_platform_user_id, updated_by_platform_user_id,
                created_at, updated_at)
            SELECT
                'b3000000-0001-4000-8000-000000000001'::uuid,
                tenants.id,
                tenants.display_name,
                'OneVerz',
                'dddddddd-0002-4000-8000-000000000001'::uuid,
                NULL,
                NULL,
                now(),
                now()
            FROM tenants
            WHERE tenants.id = '55555555-0000-4000-8000-000000000001'::uuid
            ON CONFLICT (tenant_id) DO UPDATE
            SET trading_name = EXCLUDED.trading_name,
                logo_media_asset_id = EXCLUDED.logo_media_asset_id,
                updated_at = now();
            """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The preceding target-branding migration owns this Development data.
        // Its Down operation performs the corresponding cleanup.
    }
}
