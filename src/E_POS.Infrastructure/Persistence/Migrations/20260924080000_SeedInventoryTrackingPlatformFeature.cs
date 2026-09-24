using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Root cause fix: <c>PlatformTenantFeatureCodes.InventoryTracking</c> ("inventory_tracking") has been a
/// canonical, commercially-selectable feature code since Release 1 (see
/// <c>CommercialSubscriptionFeatureCatalog</c>) and migration
/// <c>20260831120000_NormalizeCapabilityCatalogRuntimeData</c> already assumes a
/// <c>platform_features</c> row with this code exists — but no migration ever inserted it.
///
/// Effect of the gap: <see cref="E_POS.Infrastructure.Modules.Platform.Subscription.Services.TenantFeatureEntitlementEvaluator"/>
/// looks the code up in <c>platform_features</c> before evaluating any tenant entitlement. With zero rows
/// for "inventory_tracking" the evaluator always returns <c>UnknownFeature</c> (fail-closed), for every
/// tenant, whenever the Product Setup wizard enables Batch/Expiry/Serial tracking or supplies an initial
/// batch/expiry/serial value (see <c>ProductWizardAccessPolicy.HasInventoryTrackingEntitlementAsync</c>).
/// The user-visible symptom is the shared <c>product.entitlement_denied</c> error, "Product management
/// feature is not included in the tenant subscription.", even though the tenant has full Product
/// Catalog access — reproduced live via POST /api/v1/tenant-admin/products/draft with
/// <c>expiryTracking: true</c>.
///
/// This migration only completes the platform feature catalog (data-completeness fix). It intentionally
/// does NOT grant the entitlement to any tenant or subscription plan — "Inventory Tracking" is a
/// separately commercially-selectable Release-1 feature (see <c>CommercialSubscriptionFeatureCatalog</c>),
/// and bundling it into every tenant's plan is a business/commercial decision for Platform Admin to make
/// via the normal subscription plan feature management flow, not a data-repair concern.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260924080000_SeedInventoryTrackingPlatformFeature")]
public partial class SeedInventoryTrackingPlatformFeature : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Seed the missing canonical "inventory_tracking" platform feature row.
            -- Id follows the existing 72500000-0000-0000-0000-0000000000xx commercial feature id block
            -- (see product_catalog = ...004, product_categories = ...005, etc.); ...00a was unused.
            INSERT INTO platform_features (
                id, platform_module_id, feature_code, feature_key, feature_name,
                is_core_feature, name, description, status, sort_order, created_at, updated_at)
            SELECT
                '72500000-0000-0000-0000-00000000000a',
                pm.id,
                'inventory_tracking',
                'inventory_tracking',
                'Inventory Tracking',
                true,
                'Inventory Tracking',
                'Commercial entitlement for batch, expiry and serial number tracking in Product Setup and stock operations.',
                'ACTIVE',
                1,
                now(),
                now()
            FROM platform_modules pm
            WHERE pm.module_code = 'inventory'
            ON CONFLICT (feature_key) DO UPDATE
            SET feature_code = EXCLUDED.feature_code,
                feature_name = EXCLUDED.feature_name,
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = 'ACTIVE',
                is_core_feature = true,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Non-destructive rollback: retire rather than delete, in case entitlements/plan mappings
        // were created against this row after Up() ran.
        migrationBuilder.Sql("""
            UPDATE platform_features
            SET status = 'INACTIVE', updated_at = now()
            WHERE feature_code = 'inventory_tracking';
            """);
    }
}
