namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentProductImageOverridesSeedData
{
    public const string UpSql = """
        INSERT INTO product_images (
            id, tenant_id, product_id, product_variant_id, sales_channel_id,
            image_storage_key, image_url, alt_text, image_purpose, mime_type,
            file_size_bytes, width_px, height_px, checksum_hash, sort_order,
            is_primary_image, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        VALUES
            (
                'cccc0008-0005-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-0005-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-005-running-shoes.jpg',
                'https://img.magnific.com/premium-photo/pair-shoes-with-word-adidas-them_1004054-25787.jpg?w=360',
                'Running Shoes product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000a-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000a-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-010-stadium-lanyard.jpg',
                'https://cdn11.bigcommerce.com/s-pitohwmksz/images/stencil/2560w/products/3066/5035/Jersey_Logos_Lanyard__17907.1737039405.jpg?c=2',
                'Stadium Lanyard product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000b-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000b-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-011-match-football.jpg',
                'https://www.prodirectsport.ie/cdn/shop/files/1049418_main.jpg?v=1783785354',
                'Match Football product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000c-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000c-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-012-training-basketball.jpg',
                'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSEdZjG9fIGN0PiOhlrxjur3D0t9MIL12E7xazOs0t6mNlT8h7S9_du6C5_&s=10',
                'Training Basketball product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000d-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000d-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-013-water-bottle.jpg',
                'https://img.magnific.com/premium-photo/attractive-water-bottle-red_944019-12905.jpg',
                'Water Bottle product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000e-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000e-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-014-gym-bag.jpg',
                'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSywN5OJLqxgeUZ6KqRIfJ849g_96_-fpQU5D7y_Mz2aHqsy2LSjcenUfQ&s=10',
                'Gym Bag product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0008-000f-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0004-000f-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-015-silicone-wristband.jpg',
                'https://m.media-amazon.com/images/I/51l1j2z1HNL._AC_SX679_.jpg',
                'Silicone Wristband product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            ),
            (
                'cccc0026-0001-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccc0010-0001-4000-8000-000000000001',
                NULL, NULL,
                'seed/merchandise/mer-016-pro-team-jersey.jpg',
                'https://img.magnific.com/premium-vector/3d-realistic-soccer-away-jersey-germany-national-team-2024_97886-29601.jpg?semt=ais_hybrid&w=740&q=80',
                'Pro Team Jersey product image',
                'CATALOG', 'image/jpeg', NULL, NULL, NULL, NULL, 0, true, 'ACTIVE',
                '99999999-0003-4000-8000-000000000001',
                '99999999-0003-4000-8000-000000000001',
                now(), now()
            )
        ON CONFLICT (id) DO UPDATE
        SET image_storage_key = EXCLUDED.image_storage_key,
            image_url = EXCLUDED.image_url,
            alt_text = EXCLUDED.alt_text,
            image_purpose = EXCLUDED.image_purpose,
            mime_type = EXCLUDED.mime_type,
            width_px = EXCLUDED.width_px,
            height_px = EXCLUDED.height_px,
            sort_order = EXCLUDED.sort_order,
            is_primary_image = true,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM product_images
        WHERE id = 'cccc0026-0001-4000-8000-000000000001';

        UPDATE product_images AS image
        SET image_storage_key = previous.image_storage_key,
            image_url = previous.image_url,
            mime_type = 'image/png',
            width_px = 640,
            height_px = 480,
            updated_at = now()
        FROM (VALUES
            ('cccc0008-0005-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-005-running-shoes.png', 'https://placehold.co/640x480/png?text=Running+Shoes'),
            ('cccc0008-000a-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-010-stadium-lanyard.png', 'https://placehold.co/640x480/png?text=Stadium+Lanyard'),
            ('cccc0008-000b-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-011-match-football.png', 'https://placehold.co/640x480/png?text=Match+Football'),
            ('cccc0008-000c-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-012-training-basketball.png', 'https://placehold.co/640x480/png?text=Training+Basketball'),
            ('cccc0008-000d-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-013-water-bottle.png', 'https://placehold.co/640x480/png?text=Water+Bottle'),
            ('cccc0008-000e-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-014-gym-bag.png', 'https://placehold.co/640x480/png?text=Gym+Bag'),
            ('cccc0008-000f-4000-8000-000000000001'::uuid, 'seed/merchandise/mer-015-silicone-wristband.png', 'https://placehold.co/640x480/png?text=Silicone+Wristband')
        ) AS previous(id, image_storage_key, image_url)
        WHERE image.id = previous.id;
        """;
}
