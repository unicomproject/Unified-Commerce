using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds the tenant-editable POS login branding setting definitions.
/// This is a data-only migration and does not add branding tables or columns.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260810120000_SeedPosLoginBrandingSettingDefinitions")]
public sealed class SeedPosLoginBrandingSettingDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO setting_definitions (
                id,
                setting_key,
                display_name,
                value_type,
                default_value,
                description,
                is_tenant_editable,
                status,
                created_at,
                updated_at)
            VALUES
                ('a1000000-0000-4000-8000-00000000000c'::uuid, 'pos.login.system_name', 'POS login system name', 'string', '"Smart Cashier System"'::jsonb, 'System label displayed on the POS login screen.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-00000000000d'::uuid, 'pos.login.description', 'POS login description', 'string', '"Powering every sale. Every venue. Every day."'::jsonb, 'Login-specific tenant presentation copy.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-00000000000e'::uuid, 'pos.login.subtitle_template', 'POS login subtitle', 'string', '"Sign in to continue to {tenantName}"'::jsonb, 'Login subtitle template; only {tenantName} is supported.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-00000000000f'::uuid, 'pos.login.background_mode', 'POS login background mode', 'string', '"COLOR"'::jsonb, 'Active POS login background mode: IMAGE or COLOR.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000010'::uuid, 'pos.login.background_color', 'POS login background color', 'string', '"#020B1F"'::jsonb, 'POS login background color in #RRGGBB format.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000011'::uuid, 'pos.login.background_media_asset_id', 'POS login background media', 'string', 'null'::jsonb, 'Optional tenant-owned POS_LOGIN_BACKGROUND media asset UUID.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000012'::uuid, 'pos.login.hero_media_asset_id', 'POS login hero media', 'string', 'null'::jsonb, 'Optional tenant-owned POS_LOGIN_HERO media asset UUID.', TRUE, 'ACTIVE', now(), now())
            ON CONFLICT (setting_key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM setting_definitions
            WHERE setting_key IN (
                'pos.login.system_name',
                'pos.login.description',
                'pos.login.subtitle_template',
                'pos.login.background_mode',
                'pos.login.background_color',
                'pos.login.background_media_asset_id',
                'pos.login.hero_media_asset_id'
            );
            """);
    }
}
