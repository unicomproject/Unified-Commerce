using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260827090000_SeedPosThemeSettingDefinitions")]
public sealed class SeedPosThemeSettingDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO setting_definitions (
                id, setting_key, display_name, value_type, default_value,
                description, is_tenant_editable, status, created_at, updated_at)
            VALUES
                ('a1000000-0000-4000-8000-000000000013'::uuid, 'pos.theme.primary_color', 'POS primary colour', 'string', '"#FF6A00"'::jsonb, 'Primary action and emphasis colour for the authenticated POS experience.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000014'::uuid, 'pos.theme.secondary_color', 'POS secondary colour', 'string', '"#000000"'::jsonb, 'Secondary shell and navigation colour for the authenticated POS experience.', TRUE, 'ACTIVE', now(), now())
            ON CONFLICT (setting_key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM setting_definitions
            WHERE setting_key IN ('pos.theme.primary_color', 'pos.theme.secondary_color');
            """);
    }
}
