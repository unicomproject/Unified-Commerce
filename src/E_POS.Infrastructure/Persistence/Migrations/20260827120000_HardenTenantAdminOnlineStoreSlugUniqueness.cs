using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260827120000_HardenTenantAdminOnlineStoreSlugUniqueness")]
public partial class HardenTenantAdminOnlineStoreSlugUniqueness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM tenant_settings
                    WHERE NULLIF(BTRIM(setting_value::jsonb ->> 'storeSlug'), '') IS NOT NULL
                    GROUP BY LOWER(setting_value::jsonb ->> 'storeSlug')
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Duplicate online store slugs must be reconciled before applying this migration.';
                END IF;
            END $$;

            CREATE UNIQUE INDEX ux_tenant_settings_online_store_slug
                ON tenant_settings (LOWER(setting_value::jsonb ->> 'storeSlug'))
                WHERE NULLIF(BTRIM(setting_value::jsonb ->> 'storeSlug'), '') IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_tenant_settings_online_store_slug;");
    }
}
