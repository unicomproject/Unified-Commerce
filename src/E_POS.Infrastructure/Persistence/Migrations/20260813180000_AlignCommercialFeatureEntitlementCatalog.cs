using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Aligns Release-1 commercial subscription entitlements and permission feature mappings
/// with CANONICAL_PERMISSION_AND_FEATURE_ENTITLEMENT_CONTRACT_R1.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260813180000_AlignCommercialFeatureEntitlementCatalog")]
public partial class AlignCommercialFeatureEntitlementCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- 1. Ensure canonical pos_checkout commercial feature exists.
            INSERT INTO platform_features (
                id, platform_module_id, feature_code, feature_key, feature_name,
                is_core_feature, name, description, status, sort_order, created_at, updated_at)
            VALUES (
                '72000000-0000-0000-0000-000000000023',
                '71000000-0000-0000-0000-000000000010',
                'pos_checkout',
                'pos_checkout',
                'POS Checkout',
                true,
                'POS Checkout',
                'Commercial POS checkout entitlement for cashier operations.',
                'ACTIVE',
                5,
                now(),
                now()
            )
            ON CONFLICT (feature_key) DO UPDATE
            SET feature_code = EXCLUDED.feature_code,
                feature_name = EXCLUDED.feature_name,
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = 'ACTIVE',
                is_core_feature = true,
                updated_at = now();

            -- 2. Remap POS operational permissions from technical pos.* grouping features to pos_checkout.
            UPDATE permission_definitions pd
            SET feature_id = '72000000-0000-0000-0000-000000000023'::uuid,
                updated_at = now()
            WHERE pd.feature_id IN (
                '72000000-0000-0000-0000-000000000010',
                '72000000-0000-0000-0000-000000000011',
                '72000000-0000-0000-0000-000000000012',
                '72000000-0000-0000-0000-000000000013',
                '72000000-0000-0000-0000-000000000014',
                '72000000-0000-0000-0000-000000000015',
                '72000000-0000-0000-0000-000000000016',
                '72000000-0000-0000-0000-000000000017',
                '72000000-0000-0000-0000-000000000018',
                '72000000-0000-0000-0000-000000000019',
                '72000000-0000-0000-0000-000000000020',
                '72000000-0000-0000-0000-000000000022'
            );

            -- 3. Remap invalid tenant.till_ops permissions to till_management.
            UPDATE permission_definitions pd
            SET feature_id = '72500000-0000-0000-0000-000000000003'::uuid,
                updated_at = now()
            WHERE pd.feature_id = '72000000-0000-0000-0000-000000000021'::uuid;

            -- 4. Add pos_checkout to plans that previously included any technical POS feature.
            INSERT INTO subscription_plan_features (
                id, subscription_plan_id, platform_feature_id, status, created_at, updated_at)
            SELECT
                md5(sp.id::text || ':pos_checkout')::uuid,
                sp.id,
                '72000000-0000-0000-0000-000000000023'::uuid,
                'included',
                now(),
                now()
            FROM subscription_plans sp
            WHERE NOT EXISTS (
                SELECT 1
                FROM subscription_plan_features spf
                JOIN platform_features pf ON pf.id = spf.platform_feature_id
                WHERE spf.subscription_plan_id = sp.id
                  AND spf.status = 'included'
                  AND pf.feature_code = 'pos_checkout'
            )
              AND EXISTS (
                SELECT 1
                FROM subscription_plan_features spf
                JOIN platform_features pf ON pf.id = spf.platform_feature_id
                WHERE spf.subscription_plan_id = sp.id
                  AND spf.status = 'included'
                  AND pf.feature_code IN (
                    'pos.cash_drawer', 'pos.customers', 'pos.exchanges', 'pos.home',
                    'pos.notifications', 'pos.orders', 'pos.payments', 'pos.products',
                    'pos.receipts', 'pos.returns', 'pos.sales', 'pos.till'
                  )
              )
            ON CONFLICT DO NOTHING;

            -- 5. Ensure product_catalog plan mapping exists where product_* technical features were mapped.
            INSERT INTO subscription_plan_features (
                id, subscription_plan_id, platform_feature_id, status, created_at, updated_at)
            SELECT
                md5(sp.id::text || ':product_catalog')::uuid,
                sp.id,
                '72500000-0000-0000-0000-000000000004'::uuid,
                'included',
                now(),
                now()
            FROM subscription_plans sp
            WHERE NOT EXISTS (
                SELECT 1
                FROM subscription_plan_features spf
                JOIN platform_features pf ON pf.id = spf.platform_feature_id
                WHERE spf.subscription_plan_id = sp.id
                  AND spf.status = 'included'
                  AND pf.feature_code = 'product_catalog'
            )
              AND EXISTS (
                SELECT 1
                FROM subscription_plan_features spf
                JOIN platform_features pf ON pf.id = spf.platform_feature_id
                WHERE spf.subscription_plan_id = sp.id
                  AND spf.status = 'included'
                  AND pf.feature_code IN (
                    'product_barcodes', 'product_brands', 'product_categories',
                    'product_images', 'product_variants'
                  )
              )
            ON CONFLICT DO NOTHING;

            -- 6. Remove invalid commercial plan feature mappings for technical/product_* features.
            DELETE FROM subscription_plan_features spf
            USING platform_features pf
            WHERE spf.platform_feature_id = pf.id
              AND pf.feature_code IN (
                'pos.cash_drawer',
                'pos.customers',
                'pos.exchanges',
                'pos.home',
                'pos.notifications',
                'pos.orders',
                'pos.payments',
                'pos.products',
                'pos.receipts',
                'pos.returns',
                'pos.sales',
                'pos.till',
                'tenant.till_ops',
                'product_barcodes',
                'product_brands',
                'product_categories',
                'product_images',
                'product_variants'
              );

            -- 7. Retire technical grouping features from commercial selection (keep rows for audit).
            UPDATE platform_features
            SET status = 'INACTIVE',
                is_core_feature = false,
                updated_at = now()
            WHERE feature_code IN (
                'pos.cash_drawer',
                'pos.customers',
                'pos.exchanges',
                'pos.home',
                'pos.notifications',
                'pos.orders',
                'pos.payments',
                'pos.products',
                'pos.receipts',
                'pos.returns',
                'pos.sales',
                'pos.till',
                'tenant.till_ops',
                'product_barcodes',
                'product_brands',
                'product_categories',
                'product_images',
                'product_variants'
            )
              AND status = 'ACTIVE';

            -- 8. Remove invalid tenant entitlements for technical product_* features.
            DELETE FROM tenant_feature_entitlements tfe
            USING platform_features pf
            WHERE tfe.platform_feature_id = pf.id
              AND pf.feature_code IN (
                'product_barcodes',
                'product_brands',
                'product_categories',
                'product_images',
                'product_variants',
                'pos.cash_drawer',
                'pos.customers',
                'pos.exchanges',
                'pos.home',
                'pos.notifications',
                'pos.orders',
                'pos.payments',
                'pos.products',
                'pos.receipts',
                'pos.returns',
                'pos.sales',
                'pos.till',
                'tenant.till_ops'
              );

            -- 9. Ensure product_catalog tenant entitlement exists where product_* was previously granted.
            INSERT INTO tenant_feature_entitlements (
                id, tenant_id, platform_feature_id, feature_id,
                entitlement_status, source_type, is_enabled,
                effective_from, effective_until, created_at, updated_at)
            SELECT
                md5(t.id::text || ':product_catalog')::uuid,
                t.id,
                '72500000-0000-0000-0000-000000000004'::uuid,
                '72500000-0000-0000-0000-000000000004'::uuid,
                'ENABLED',
                'MIGRATION',
                true,
                now(),
                NULL,
                now(),
                now()
            FROM tenants t
            WHERE NOT EXISTS (
                SELECT 1
                FROM tenant_feature_entitlements existing
                JOIN platform_features pf ON pf.id = existing.platform_feature_id
                WHERE existing.tenant_id = t.id
                  AND pf.feature_code = 'product_catalog'
                  AND existing.is_enabled = true
            )
              AND EXISTS (
                SELECT 1
                FROM tenant_feature_entitlements legacy
                JOIN platform_features pf ON pf.id = legacy.platform_feature_id
                WHERE legacy.tenant_id = t.id
                  AND pf.feature_code IN (
                    'product_barcodes', 'product_brands', 'product_categories',
                    'product_images', 'product_variants'
                  )
              )
            ON CONFLICT (tenant_id, platform_feature_id) DO UPDATE
            SET entitlement_status = 'ENABLED',
                is_enabled = true,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Non-destructive rollback: do not reactivate invalid commercial mappings.
    }
}
