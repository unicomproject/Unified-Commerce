using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260809210000_SeedAllTenantsProductCatalogEntitlement")]
public partial class SeedAllTenantsProductCatalogEntitlement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO platform_modules (
                id, module_code, name, description, status, sort_order,
                module_key, module_name, is_core_module, created_at, updated_at
            )
            VALUES (
                '71500000-0000-0000-0000-000000000004',
                'product_management',
                'Product Management',
                'Product management and catalog capabilities.',
                'ACTIVE',
                4,
                'product_management',
                'Product Management',
                true,
                now(),
                now()
            )
            ON CONFLICT (module_code) DO UPDATE
            SET name = EXCLUDED.name,
                module_key = EXCLUDED.module_key,
                module_name = EXCLUDED.module_name,
                status = 'ACTIVE',
                updated_at = now();

            INSERT INTO platform_features (
                id, platform_module_id, feature_code, name, description,
                status, sort_order, feature_key, feature_name, is_core_feature, created_at, updated_at
            )
            VALUES
                ('72500000-0000-0000-0000-000000000004', '71500000-0000-0000-0000-000000000004', 'product_catalog', 'Product Catalog', 'Product Catalog Feature', 'ACTIVE', 1, 'product_catalog', 'Product Catalog', true, now(), now()),
                ('72500000-0000-0000-0000-000000000005', '71500000-0000-0000-0000-000000000004', 'product_categories', 'Categories & Departments', 'Categories Feature', 'ACTIVE', 2, 'product_categories', 'Categories & Departments', true, now(), now()),
                ('72500000-0000-0000-0000-000000000006', '71500000-0000-0000-0000-000000000004', 'product_brands', 'Brand Management', 'Brands Feature', 'ACTIVE', 3, 'product_brands', 'Brand Management', true, now(), now()),
                ('72500000-0000-0000-0000-000000000007', '71500000-0000-0000-0000-000000000004', 'product_barcodes', 'Product Barcodes', 'Barcodes Feature', 'ACTIVE', 4, 'product_barcodes', 'Product Barcodes', true, now(), now()),
                ('72500000-0000-0000-0000-000000000008', '71500000-0000-0000-0000-000000000004', 'product_images', 'Product Images', 'Images Feature', 'ACTIVE', 5, 'product_images', 'Product Images', true, now(), now()),
                ('72500000-0000-0000-0000-000000000009', '71500000-0000-0000-0000-000000000004', 'product_variants', 'Product Variants', 'Variants Feature', 'ACTIVE', 6, 'product_variants', 'Product Variants', true, now(), now())
            ON CONFLICT (platform_module_id, feature_code) DO UPDATE
            SET name = EXCLUDED.name,
                feature_key = EXCLUDED.feature_key,
                feature_name = EXCLUDED.feature_name,
                status = 'ACTIVE',
                updated_at = now();

            INSERT INTO tenant_feature_entitlements (
                id, tenant_id, platform_feature_id, feature_id,
                entitlement_status, source_type, is_enabled,
                effective_from, effective_until, created_at, updated_at
            )
            SELECT
                md5(t.id::text || ':' || pf.id::text)::uuid,
                t.id,
                pf.id,
                pf.id,
                'ENABLED',
                'MANUAL',
                true,
                now(),
                NULL,
                now(),
                now()
            FROM tenants t
            CROSS JOIN platform_features pf
            WHERE pf.platform_module_id = '71500000-0000-0000-0000-000000000004'
            ON CONFLICT (tenant_id, platform_feature_id) DO UPDATE
            SET entitlement_status = 'ENABLED',
                is_enabled = true,
                effective_until = NULL,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM tenant_feature_entitlements
            WHERE platform_feature_id IN (
                '72500000-0000-0000-0000-000000000004',
                '72500000-0000-0000-0000-000000000005',
                '72500000-0000-0000-0000-000000000006',
                '72500000-0000-0000-0000-000000000007',
                '72500000-0000-0000-0000-000000000008',
                '72500000-0000-0000-0000-000000000009'
            );
            """);
    }
}
