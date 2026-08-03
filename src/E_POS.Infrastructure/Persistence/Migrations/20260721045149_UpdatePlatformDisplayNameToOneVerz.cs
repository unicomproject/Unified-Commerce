using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class UpdatePlatformDisplayNameToOneVerz : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Branding-only: update legacy default display name when still at original seed value.
        migrationBuilder.Sql("""
            UPDATE platform_settings
            SET setting_value = '"OneVerz"'::jsonb,
                updated_at = now()
            WHERE setting_key = 'general.platform_display_name'
              AND setting_value::text IN ('"SCS-TIX"', '"SCS TIX"', '"SCSTIX"');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE platform_settings
            SET setting_value = '"SCS-TIX"'::jsonb,
                updated_at = now()
            WHERE setting_key = 'general.platform_display_name'
              AND setting_value::text = '"OneVerz"';
            """);
    }
}
