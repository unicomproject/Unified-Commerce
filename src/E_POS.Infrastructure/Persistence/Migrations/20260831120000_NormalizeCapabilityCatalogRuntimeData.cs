using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260831120000_NormalizeCapabilityCatalogRuntimeData")]
public partial class NormalizeCapabilityCatalogRuntimeData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- 1. Clean up Smoke test records and dependencies
            DELETE FROM tenant_feature_entitlements
            WHERE platform_feature_id IN (
                SELECT id FROM platform_features WHERE feature_code LIKE '%smoke%' OR name LIKE '%Smoke%'
            );

            DELETE FROM platform_permissions
            WHERE platform_feature_id IN (
                SELECT id FROM platform_features WHERE feature_code LIKE '%smoke%' OR name LIKE '%Smoke%'
            );

            DELETE FROM platform_features
            WHERE feature_code LIKE '%smoke%' OR name LIKE '%Smoke%';

            DELETE FROM platform_modules
            WHERE module_code LIKE '%smoke%' OR name LIKE '%Smoke%';

            -- 2. Remap 35 active tenant permissions from Legacy Permission Module (00000000-0000-0000-0000-000000000000) to canonical features
            -- A. Inventory Tracking permissions
            UPDATE permission_definitions
            SET module_id = COALESCE((SELECT platform_module_id FROM platform_features WHERE feature_code = 'inventory_tracking' LIMIT 1), (SELECT id FROM platform_modules LIMIT 1)),
                feature_id = COALESCE((SELECT id FROM platform_features WHERE feature_code = 'inventory_tracking' LIMIT 1), (SELECT id FROM platform_features LIMIT 1)),
                action_type = CASE WHEN permission_code LIKE '%.view' THEN 'view' ELSE 'create' END,
                updated_at = now()
            WHERE permission_code IN ('tenant.stock.dashboard.view', 'tenant.stock.opening')
              AND (module_id = '00000000-0000-0000-0000-000000000000' OR feature_id = '00000000-0000-0000-0000-000000000000');

            -- B. Sales Orders / Fulfillment permissions
            UPDATE permission_definitions
            SET module_id = COALESCE((SELECT platform_module_id FROM platform_features WHERE feature_code = 'sales_orders' LIMIT 1), (SELECT id FROM platform_modules LIMIT 1)),
                feature_id = COALESCE((SELECT id FROM platform_features WHERE feature_code = 'sales_orders' LIMIT 1), (SELECT id FROM platform_features LIMIT 1)),
                action_type = CASE WHEN permission_code LIKE '%.view' THEN 'view' ELSE 'manage' END,
                updated_at = now()
            WHERE permission_code IN ('fulfillment.orders.view', 'fulfillment.orders.manage')
              AND (module_id = '00000000-0000-0000-0000-000000000000' OR feature_id = '00000000-0000-0000-0000-000000000000');

            -- C. Hardware Device Management permissions
            UPDATE permission_definitions
            SET module_id = COALESCE((SELECT platform_module_id FROM platform_features WHERE feature_code = 'hardware_device_management' LIMIT 1), (SELECT id FROM platform_modules LIMIT 1)),
                feature_id = COALESCE((SELECT id FROM platform_features WHERE feature_code = 'hardware_device_management' LIMIT 1), (SELECT id FROM platform_features LIMIT 1)),
                action_type = CASE 
                    WHEN permission_code LIKE '%.view' THEN 'view'
                    WHEN permission_code LIKE '%.create' THEN 'create'
                    WHEN permission_code LIKE '%.update' THEN 'update'
                    WHEN permission_code LIKE '%.delete' THEN 'delete'
                    ELSE 'manage'
                END,
                updated_at = now()
            WHERE permission_code LIKE 'tenant.devices.%'
              AND (module_id = '00000000-0000-0000-0000-000000000000' OR feature_id = '00000000-0000-0000-0000-000000000000');

            -- D. Product Catalog / Pricing & Tax permissions
            UPDATE permission_definitions
            SET module_id = COALESCE((SELECT platform_module_id FROM platform_features WHERE feature_code = 'product_catalog' LIMIT 1), (SELECT id FROM platform_modules LIMIT 1)),
                feature_id = COALESCE((SELECT id FROM platform_features WHERE feature_code = 'product_catalog' LIMIT 1), (SELECT id FROM platform_features LIMIT 1)),
                action_type = CASE 
                    WHEN permission_code LIKE '%.view' THEN 'view'
                    WHEN permission_code LIKE '%.create' THEN 'create'
                    WHEN permission_code LIKE '%.update' THEN 'update'
                    WHEN permission_code LIKE '%.delete' THEN 'delete'
                    ELSE 'manage'
                END,
                updated_at = now()
            WHERE (permission_code LIKE 'pricing.price_lists.%' OR permission_code LIKE 'tax.classes.%' OR permission_code LIKE 'tax.rates.%')
              AND (module_id = '00000000-0000-0000-0000-000000000000' OR feature_id = '00000000-0000-0000-0000-000000000000');

            -- E. Tenant Settings permission
            UPDATE permission_definitions
            SET module_id = COALESCE((SELECT platform_module_id FROM platform_features WHERE feature_code = 'tenant_settings' LIMIT 1), (SELECT id FROM platform_modules LIMIT 1)),
                feature_id = COALESCE((SELECT id FROM platform_features WHERE feature_code = 'tenant_settings' LIMIT 1), (SELECT id FROM platform_features LIMIT 1)),
                action_type = 'manage',
                updated_at = now()
            WHERE permission_code = 'tenant.settings.manage'
              AND (module_id = '00000000-0000-0000-0000-000000000000' OR feature_id = '00000000-0000-0000-0000-000000000000');

            -- F. Fallback mapping for any remaining orphan tenant permissions
            UPDATE permission_definitions pd
            SET module_id = COALESCE((SELECT platform_module_id FROM platform_features WHERE feature_code = 'product_catalog' LIMIT 1), (SELECT id FROM platform_modules LIMIT 1)),
                feature_id = COALESCE((SELECT id FROM platform_features WHERE feature_code = 'product_catalog' LIMIT 1), (SELECT id FROM platform_features LIMIT 1)),
                updated_at = now()
            WHERE pd.module_id = '00000000-0000-0000-0000-000000000000' OR pd.feature_id = '00000000-0000-0000-0000-000000000000';

            -- 3. Deactivate zero-feature navigation placeholder modules
            UPDATE platform_modules
            SET status = 'INACTIVE', updated_at = now()
            WHERE module_code IN ('dashboard', 'inventory', 'outlets', 'products', 'reports', 'sales_pos', 'tills', 'users')
              AND NOT EXISTS (
                  SELECT 1 FROM platform_features pf WHERE pf.platform_module_id = platform_modules.id AND pf.status = 'ACTIVE'
              );

            -- 4. Deactivate empty Legacy Permission Module
            UPDATE platform_modules
            SET status = 'INACTIVE', updated_at = now()
            WHERE module_code LIKE 'legacy_permission_module_%';

            UPDATE platform_features
            SET status = 'INACTIVE', updated_at = now()
            WHERE feature_code LIKE 'legacy_permission_feature_%';

            -- 5. Backfill action_type for all active permissions where action_type is blank or null
            UPDATE permission_definitions
            SET action_type = CASE
                WHEN permission_code LIKE '%.view' OR permission_code LIKE '%.read' OR permission_code LIKE '%.list' THEN 'view'
                WHEN permission_code LIKE '%.create' OR permission_code LIKE '%.add' OR permission_code LIKE '%.opening' THEN 'create'
                WHEN permission_code LIKE '%.update' OR permission_code LIKE '%.edit' THEN 'update'
                WHEN permission_code LIKE '%.delete' OR permission_code LIKE '%.remove' THEN 'delete'
                WHEN permission_code LIKE '%.manage' OR permission_code LIKE '%.publish' THEN 'manage'
                WHEN permission_code LIKE '%.adjust' THEN 'adjust'
                WHEN permission_code LIKE '%.assign' THEN 'assign'
                ELSE 'manage'
            END
            WHERE is_active = true AND (action_type IS NULL OR action_type = '');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Up migration contains safe, idempotent data normalizations
    }
}
