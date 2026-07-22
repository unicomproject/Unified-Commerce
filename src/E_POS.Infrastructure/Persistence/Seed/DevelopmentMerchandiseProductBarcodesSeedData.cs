namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentMerchandiseProductBarcodesSeedData
{
    public const string UpSql = """
        WITH seeded_variants AS (
            SELECT
                variant.id AS variant_id,
                variant.tenant_id,
                variant.product_id,
                variant.sales_uom_id,
                product.product_code,
                row_number() OVER (
                    PARTITION BY product.id
                    ORDER BY variant.is_default_variant DESC, variant.variant_code, variant.id
                ) AS variant_number
            FROM product_variants AS variant
            JOIN products AS product
              ON product.id = variant.product_id
             AND product.tenant_id = variant.tenant_id
            WHERE variant.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
              AND product.product_code IN (
                  'MER-001', 'MER-002', 'MER-003', 'MER-004', 'MER-005',
                  'MER-006', 'MER-007', 'MER-008', 'MER-009', 'MER-010',
                  'MER-011', 'MER-012', 'MER-013', 'MER-014', 'MER-015'
              )
              AND variant.status = 'ACTIVE'
        ),
        barcode_bases AS (
            SELECT
                seeded_variants.*,
                '2000000'
                    || lpad(regexp_replace(product_code, '[^0-9]', '', 'g'), 4, '0')
                    || variant_number::text AS base_code
            FROM seeded_variants
        ),
        barcode_values AS (
            SELECT
                barcode_bases.*,
                base_code || (
                    (10 - (
                        SELECT sum(
                            digit::integer * CASE WHEN position % 2 = 1 THEN 1 ELSE 3 END
                        ) % 10
                        FROM regexp_split_to_table(base_code, '')
                            WITH ORDINALITY AS digits(digit, position)
                    )) % 10
                )::text AS barcode
            FROM barcode_bases
        )
        INSERT INTO product_barcodes (
            id, tenant_id, product_id, product_variant_id, barcode, barcode_type,
            uom_id, quantity_per_scan, is_primary_barcode, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            ('bbbb' || substring(variant_id::text, 5))::uuid,
            tenant_id,
            product_id,
            variant_id,
            barcode,
            'EAN13',
            sales_uom_id,
            1,
            true,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001'::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            now(),
            now()
        FROM barcode_values
        ON CONFLICT (tenant_id, barcode) DO UPDATE
        SET product_id = EXCLUDED.product_id,
            product_variant_id = EXCLUDED.product_variant_id,
            barcode_type = EXCLUDED.barcode_type,
            uom_id = EXCLUDED.uom_id,
            quantity_per_scan = EXCLUDED.quantity_per_scan,
            is_primary_barcode = EXCLUDED.is_primary_barcode,
            status = 'ACTIVE',
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM product_barcodes
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND id::text LIKE 'bbbb%'
          AND product_id IN (
              SELECT id
              FROM products
              WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
                AND product_code IN (
                    'MER-001', 'MER-002', 'MER-003', 'MER-004', 'MER-005',
                    'MER-006', 'MER-007', 'MER-008', 'MER-009', 'MER-010',
                    'MER-011', 'MER-012', 'MER-013', 'MER-014', 'MER-015'
                )
          );
        """;
}
