using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Removes the redundant POS suffix from the default template because tenant
/// display names may already contain POS. Existing custom templates are kept.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260810120300_CorrectPosLoginBrandingSubtitleTemplate")]
public sealed class CorrectPosLoginBrandingSubtitleTemplate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            UPDATE setting_definitions
            SET default_value = '"Sign in to continue to {tenantName}"'::jsonb,
                updated_at = now()
            WHERE setting_key = 'pos.login.subtitle_template'
              AND default_value = '"Sign in to continue to {tenantName} POS"'::jsonb;

            UPDATE tenant_settings AS tenant_setting
            SET setting_value = '"Sign in to continue to {tenantName}"'::jsonb,
                updated_at = now()
            FROM setting_definitions AS definition
            WHERE tenant_setting.setting_definition_id = definition.id
              AND definition.setting_key = 'pos.login.subtitle_template'
              AND tenant_setting.setting_value = '"Sign in to continue to {tenantName} POS"'::jsonb;
            """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            UPDATE setting_definitions
            SET default_value = '"Sign in to continue to {tenantName} POS"'::jsonb,
                updated_at = now()
            WHERE setting_key = 'pos.login.subtitle_template'
              AND default_value = '"Sign in to continue to {tenantName}"'::jsonb;
            """);
}
