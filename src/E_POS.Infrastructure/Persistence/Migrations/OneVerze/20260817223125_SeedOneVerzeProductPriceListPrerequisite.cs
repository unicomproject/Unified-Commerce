using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.Infrastructure.Persistence.Migrations.OneVerze;

[DbContext(typeof(EPosDbContext))]
[Migration("20260817223125_SeedOneVerzeProductPriceListPrerequisite")]
public sealed class SeedOneVerzeProductPriceListPrerequisite : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Must precede the immutable SeedCustomProducts migration on fresh databases.
        // Existing databases retain their price-list settings and default selection.
        migrationBuilder.Sql("""
            INSERT INTO price_lists (
                id, tenant_id, price_list_code, price_list_name, price_list_type,
                currency_code, is_default_price_list, price_includes_tax, priority,
                status, created_at, updated_at)
            SELECT
                'cccc0003-0001-4000-8000-000000000002', tenant.id,
                'ONEVERZE-POS', 'OneVerze Development POS Price List', 'POS',
                tenant.base_currency_code, false, true, 0, 'ACTIVE', now(), now()
            FROM tenants AS tenant
            WHERE tenant.id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO inventory_locations (
                id, tenant_id, outlet_id, location_code, location_name, location_type,
                is_sellable_location, is_return_location, is_receiving_location,
                is_quarantine_location, status, created_at, updated_at)
            SELECT
                '33333333-0002-4000-8000-000000000001', outlet.tenant_id, outlet.id,
                'OVZ-MAIN-STOCK', 'OneVerze Development Main Stock', 'STORE',
                true, false, true, false, 'ACTIVE', now(), now()
            FROM outlets AS outlet
            WHERE outlet.tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
              AND outlet.id = '22222222-0001-4000-8000-000000000001'
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Historical product seeds reference this principal; preserve their foreign keys.
    }
}
