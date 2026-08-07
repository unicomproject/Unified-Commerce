using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Seeds Phase 4 MVP rows into <c>setting_definitions</c> only.
/// No schema DDL. Tenant settings are provisioned at finalize time.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260807120000_SeedPhase4DefaultTenantSettingDefinitions")]
public sealed class SeedPhase4DefaultTenantSettingDefinitions : Migration
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
                ('a1000000-0000-4000-8000-000000000001'::uuid, 'tax.pricing_mode', 'Tax pricing mode', 'string', '"TAX_EXCLUSIVE"'::jsonb, 'Default tax pricing mode for tenant sales (TAX_EXCLUSIVE or TAX_INCLUSIVE).', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000002'::uuid, 'locale.date_format', 'Date format', 'string', '"yyyy-MM-dd"'::jsonb, 'Default date display format.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000003'::uuid, 'locale.time_format', 'Time format', 'string', '"HH:mm"'::jsonb, 'Default time display format.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000004'::uuid, 'locale.number_format', 'Number format locale', 'string', '"en-LK"'::jsonb, 'Locale tag used for number formatting.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000005'::uuid, 'receipt.defaults', 'Receipt defaults', 'object', '{"headerText":null,"footerText":"Thank you for shopping with us.","showTaxBreakdown":true}'::jsonb, 'MVP receipt policy defaults (not a full template graph).', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000006'::uuid, 'numbering.policies', 'Numbering policies', 'object', '{"SALES_ORDER":{"prefix":"ORD-","paddingLength":6,"resetRule":"NONE"},"POS_RECEIPT":{"prefix":"RCPT-","paddingLength":6,"resetRule":"NONE"},"RETURN":{"prefix":"RET-","paddingLength":6,"resetRule":"NONE"}}'::jsonb, 'MVP document numbering policies (not sequence rows).', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000007'::uuid, 'notification.defaults', 'Notification defaults', 'object', '{"emailEnabled":true,"smsEnabled":false}'::jsonb, 'Minimal notification preference defaults.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000008'::uuid, 'security.session_policy', 'Session policy', 'object', '{"idleTimeoutMinutes":30}'::jsonb, 'Tenant-level session idle policy defaults.', FALSE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-000000000009'::uuid, 'branding.placeholders', 'Branding placeholders', 'object', '{"logoAssetId":null,"primaryColor":null}'::jsonb, 'Minimal branding placeholder defaults.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-00000000000a'::uuid, 'inventory.stock_behaviour', 'Inventory stock behaviour', 'object', '{"allowNegativeStock":false}'::jsonb, 'Inventory stock behaviour defaults.', TRUE, 'ACTIVE', now(), now()),
                ('a1000000-0000-4000-8000-00000000000b'::uuid, 'online_store.defaults', 'Online store defaults', 'object', '{"storeStatus":"DRAFT","taxDisplayMode":"MATCH_TENANT"}'::jsonb, 'Online store operational defaults.', TRUE, 'ACTIVE', now(), now())
            ON CONFLICT (setting_key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM setting_definitions
            WHERE setting_key IN (
                'tax.pricing_mode',
                'locale.date_format',
                'locale.time_format',
                'locale.number_format',
                'receipt.defaults',
                'numbering.policies',
                'notification.defaults',
                'security.session_policy',
                'branding.placeholders',
                'inventory.stock_behaviour',
                'online_store.defaults'
            );
            """);
    }
}
