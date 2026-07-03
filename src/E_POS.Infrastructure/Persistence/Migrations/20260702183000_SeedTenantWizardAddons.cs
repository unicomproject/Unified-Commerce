using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260702183000_SeedTenantWizardAddons")]
public partial class SeedTenantWizardAddons : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            INSERT INTO feature_limit_definitions (id, platform_feature_id, limit_code, name, default_limit_value, created_at, updated_at)
            VALUES
                ('74000000-0000-0000-0000-000000000001', '{SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId}', 'MAX_OUTLETS', 'Maximum Outlets', 0, now(), now()),
                ('74000000-0000-0000-0000-000000000002', '{SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId}', 'MAX_TILLS', 'Maximum Tills', 0, now(), now()),
                ('74000000-0000-0000-0000-000000000003', '{SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId}', 'MAX_USERS', 'Maximum Users', 0, now(), now())
            ON CONFLICT (platform_feature_id, limit_code) DO UPDATE
            SET name = EXCLUDED.name,
                default_limit_value = EXCLUDED.default_limit_value,
                updated_at = now();

            INSERT INTO subscription_addons (id, addon_code, name, description, status, price_amount, created_at, updated_at)
            VALUES
                ('73000000-0000-0000-0000-000000000001', 'EXTRA_OUTLET', 'Extra Outlet', 'Adds one additional outlet to tenant limits.', 'ACTIVE', 10.00, now(), now()),
                ('73000000-0000-0000-0000-000000000002', 'EXTRA_TILL', 'Extra Till', 'Adds one additional till to tenant limits.', 'ACTIVE', 5.00, now(), now()),
                ('73000000-0000-0000-0000-000000000003', 'EXTRA_USER', 'Extra User', 'Adds one additional user to tenant limits.', 'ACTIVE', 2.50, now(), now())
            ON CONFLICT (addon_code) DO UPDATE
            SET name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = 'ACTIVE',
                price_amount = EXCLUDED.price_amount,
                updated_at = now();

            INSERT INTO subscription_addon_features (id, subscription_addon_id, platform_feature_id, status, sort_order, description, created_at, updated_at)
            VALUES
                ('73100000-0000-0000-0000-000000000001', '73000000-0000-0000-0000-000000000001', '{SubscriptionBillingCatalogSeedConstants.OnlineStoreFeatureId}', 'ACTIVE', 1, 'Extra outlet increment', now(), now()),
                ('73100000-0000-0000-0000-000000000002', '73000000-0000-0000-0000-000000000002', '{SubscriptionBillingCatalogSeedConstants.OfflineSyncFeatureId}', 'ACTIVE', 1, 'Extra till increment', now(), now()),
                ('73100000-0000-0000-0000-000000000003', '73000000-0000-0000-0000-000000000003', '{SubscriptionBillingCatalogSeedConstants.ClickCollectFeatureId}', 'ACTIVE', 1, 'Extra user increment', now(), now())
            ON CONFLICT (subscription_addon_id, platform_feature_id) DO UPDATE
            SET status = 'ACTIVE',
                sort_order = EXCLUDED.sort_order,
                description = EXCLUDED.description,
                updated_at = now();

            INSERT INTO subscription_addon_limits (id, subscription_addon_feature_id, feature_limit_definition_id, limit_value, created_at, updated_at)
            VALUES
                ('73200000-0000-0000-0000-000000000001', '73100000-0000-0000-0000-000000000001', '74000000-0000-0000-0000-000000000001', 1, now(), now()),
                ('73200000-0000-0000-0000-000000000002', '73100000-0000-0000-0000-000000000002', '74000000-0000-0000-0000-000000000002', 1, now(), now()),
                ('73200000-0000-0000-0000-000000000003', '73100000-0000-0000-0000-000000000003', '74000000-0000-0000-0000-000000000003', 1, now(), now())
            ON CONFLICT (subscription_addon_feature_id, feature_limit_definition_id) DO UPDATE
            SET limit_value = EXCLUDED.limit_value,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM subscription_addon_limits
            WHERE id IN (
                '73200000-0000-0000-0000-000000000001',
                '73200000-0000-0000-0000-000000000002',
                '73200000-0000-0000-0000-000000000003'
            );

            DELETE FROM subscription_addon_features
            WHERE id IN (
                '73100000-0000-0000-0000-000000000001',
                '73100000-0000-0000-0000-000000000002',
                '73100000-0000-0000-0000-000000000003'
            );

            DELETE FROM subscription_addons
            WHERE addon_code IN ('EXTRA_OUTLET', 'EXTRA_TILL', 'EXTRA_USER');

            DELETE FROM feature_limit_definitions
            WHERE id IN (
                '74000000-0000-0000-0000-000000000001',
                '74000000-0000-0000-0000-000000000002',
                '74000000-0000-0000-0000-000000000003'
            );
            """);
    }
}
