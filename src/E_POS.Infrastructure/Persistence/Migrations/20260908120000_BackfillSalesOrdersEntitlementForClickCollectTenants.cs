using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260908120000_BackfillSalesOrdersEntitlementForClickCollectTenants")]
public sealed class BackfillSalesOrdersEntitlementForClickCollectTenants : Migration
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
                md5(tenants.id::text || ':' || sales_orders.id::text)::uuid,
                tenants.id,
                sales_orders.id,
                sales_orders.id,
                'ENABLED',
                'MANUAL',
                TRUE,
                now(),
                NULL,
                now(),
                now()
            FROM tenants
            JOIN platform_features sales_orders
              ON sales_orders.feature_code = 'sales_orders'
             AND sales_orders.status = 'ACTIVE'
            WHERE EXISTS (
                SELECT 1
                FROM tenant_feature_entitlements click_collect_entitlement
                JOIN platform_features click_collect
                  ON click_collect.id = click_collect_entitlement.platform_feature_id
                 AND click_collect.feature_code = 'click_collect'
                 AND click_collect.status = 'ACTIVE'
                WHERE click_collect_entitlement.tenant_id = tenants.id
                  AND click_collect_entitlement.entitlement_status = 'ENABLED'
                  AND click_collect_entitlement.is_enabled = TRUE
                  AND click_collect_entitlement.revoked_at IS NULL
                  AND click_collect_entitlement.effective_from <= now()
                  AND (
                      click_collect_entitlement.effective_until IS NULL
                      OR click_collect_entitlement.effective_until > now()
                  )
            )
              AND NOT EXISTS (
                  SELECT 1
                  FROM tenant_feature_entitlements existing_sales_orders
                  WHERE existing_sales_orders.tenant_id = tenants.id
                    AND existing_sales_orders.platform_feature_id = sales_orders.id
              )
            ON CONFLICT (tenant_id, platform_feature_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentional no-op. The entitlement may have been changed through
        // subscription administration after this dependency repair ran.
    }
}
