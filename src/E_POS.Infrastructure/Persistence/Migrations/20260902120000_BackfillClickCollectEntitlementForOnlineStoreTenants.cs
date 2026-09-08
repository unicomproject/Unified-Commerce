using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260902120000_BackfillClickCollectEntitlementForOnlineStoreTenants")]
public partial class BackfillClickCollectEntitlementForOnlineStoreTenants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO tenant_feature_entitlements (
                id,
                tenant_id,
                platform_feature_id,
                feature_id,
                entitlement_status,
                source_type,
                is_enabled,
                effective_from,
                effective_until,
                created_at,
                updated_at
            )
            SELECT
                md5(tenants.id::text || ':' || click_collect.id::text)::uuid,
                tenants.id,
                click_collect.id,
                click_collect.id,
                'ENABLED',
                'MANUAL',
                TRUE,
                now(),
                NULL,
                now(),
                now()
            FROM tenants
            JOIN platform_features click_collect
              ON click_collect.feature_code = 'click_collect'
             AND click_collect.status = 'ACTIVE'
            WHERE EXISTS (
                SELECT 1
                FROM tenant_feature_entitlements online_store_entitlement
                JOIN platform_features online_store
                  ON online_store.id = online_store_entitlement.platform_feature_id
                 AND online_store.feature_code = 'online_store'
                 AND online_store.status = 'ACTIVE'
                WHERE online_store_entitlement.tenant_id = tenants.id
                  AND online_store_entitlement.entitlement_status = 'ENABLED'
                  AND online_store_entitlement.is_enabled = TRUE
                  AND online_store_entitlement.revoked_at IS NULL
                  AND online_store_entitlement.effective_from <= now()
                  AND (
                      online_store_entitlement.effective_until IS NULL
                      OR online_store_entitlement.effective_until > now()
                  )
            )
              AND NOT EXISTS (
                  SELECT 1
                  FROM tenant_feature_entitlements existing_click_collect
                  WHERE existing_click_collect.tenant_id = tenants.id
                    AND existing_click_collect.platform_feature_id = click_collect.id
              )
            ON CONFLICT (tenant_id, platform_feature_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentional no-op. A commercial entitlement may have been changed
        // independently after this repair was applied and must not be revoked.
    }
}
