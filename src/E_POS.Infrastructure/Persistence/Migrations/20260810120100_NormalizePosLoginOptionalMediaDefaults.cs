using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Represents an unset optional media UUID as an empty JSON string so the
/// existing typed tenant-settings provisioner can validate new tenants.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260810120100_NormalizePosLoginOptionalMediaDefaults")]
public sealed class NormalizePosLoginOptionalMediaDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE setting_definitions
            SET default_value = '""'::jsonb,
                updated_at = now()
            WHERE setting_key IN (
                'pos.login.background_media_asset_id',
                'pos.login.hero_media_asset_id'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE setting_definitions
            SET default_value = 'null'::jsonb,
                updated_at = now()
            WHERE setting_key IN (
                'pos.login.background_media_asset_id',
                'pos.login.hero_media_asset_id'
            );
            """);
    }
}
