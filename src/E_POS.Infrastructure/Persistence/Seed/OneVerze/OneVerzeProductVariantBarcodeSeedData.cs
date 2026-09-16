namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;

/// <summary>
/// The OneVerze catalog seed never populated <c>product_barcodes</c>, so every variant had zero
/// active primary barcodes. Storefront checkout confirmation requires exactly one primary barcode
/// per line (see StorefrontCheckoutConfirmationRepository.TryResolvePrimaryBarcodeSnapshot), which
/// blocked every checkout attempt for this tenant regardless of payment method.
/// </summary>
public static class OneVerzeProductVariantBarcodeSeedData
{
    private const string TenantId = "08b0c8b0-a5bf-44f0-8814-cb2fe0120000";
    private const string OneVerzeAdminUserId = "33333333-0001-4000-8000-000000000001";
    private const string DefaultUomId = "91000000-0000-4000-8000-000000000001";

    public const string UpSql = $"""
        INSERT INTO product_barcodes (
            id, tenant_id, product_id, product_variant_id, barcode, barcode_type,
            is_primary_barcode, quantity_per_scan, status, uom_id,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            md5('onverze-variant-barcode:' || pv.id::text)::uuid,
            pv.tenant_id,
            pv.product_id,
            pv.id,
            lpad((3000000000000 + row_number() OVER (ORDER BY pv.id))::text, 13, '0'),
            'EAN13',
            true,
            1.0,
            'ACTIVE',
            '{DefaultUomId}',
            '{OneVerzeAdminUserId}',
            '{OneVerzeAdminUserId}',
            now(),
            now()
        FROM product_variants pv
        WHERE pv.tenant_id = '{TenantId}'
          AND NOT EXISTS (
              SELECT 1 FROM product_barcodes pb
              WHERE pb.tenant_id = pv.tenant_id
                AND pb.product_variant_id = pv.id
                AND pb.is_primary_barcode = true
                AND pb.status <> 'DELETED'
          )
        ON CONFLICT (id) DO NOTHING;
        """;

    public const string DownSql = $"""
        DELETE FROM product_barcodes
        WHERE tenant_id = '{TenantId}'
          AND id IN (
              SELECT md5('onverze-variant-barcode:' || pv.id::text)::uuid
              FROM product_variants pv
              WHERE pv.tenant_id = '{TenantId}'
          );
        """;
}
