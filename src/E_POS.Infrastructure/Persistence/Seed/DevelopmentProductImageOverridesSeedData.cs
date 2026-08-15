namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentProductImageOverridesSeedData
{
    public const string UpSql = """
        INSERT INTO product_images (
            id, tenant_id, product_id, product_variant_id, sales_channel_id,
            alt_text, image_purpose, sort_order,
            is_primary_image, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        VALUES
            (
                'cccc0008-000a-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000a-4000-8000-000000000001',
                NULL, NULL,
                'Stadium Lanyard product image',
                'CATALOG', 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000b-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000b-4000-8000-000000000001',
                NULL, NULL,
                'Match Football product image',
                'CATALOG', 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000c-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000c-4000-8000-000000000001',
                NULL, NULL,
                'Training Basketball product image',
                'CATALOG', 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000d-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000d-4000-8000-000000000001',
                NULL, NULL,
                'Water Bottle product image',
                'CATALOG', 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000f-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000f-4000-8000-000000000001',
                NULL, NULL,
                'Silicone Wristband product image',
                'CATALOG', 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0026-0001-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0010-0001-4000-8000-000000000001',
                NULL, NULL,
                'Pro Team Jersey product image',
                'CATALOG', 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            )
        ON CONFLICT (id) DO UPDATE
        SET alt_text = EXCLUDED.alt_text,
            image_purpose = EXCLUDED.image_purpose,
            sort_order = EXCLUDED.sort_order,
            is_primary_image = true,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM product_images
        WHERE id IN (
            'cccc0026-0001-4000-8000-000000000001',
            'cccc0008-000a-4000-8000-000000000001',
            'cccc0008-000b-4000-8000-000000000001',
            'cccc0008-000c-4000-8000-000000000001',
            'cccc0008-000d-4000-8000-000000000001',
            'cccc0008-000f-4000-8000-000000000001'
        );
        """;
}
