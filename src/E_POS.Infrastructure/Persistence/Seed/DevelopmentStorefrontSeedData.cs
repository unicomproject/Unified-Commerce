namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentStorefrontSeedData
{
    public const string UpSql = """
        -- 1. Storefront Banners
        INSERT INTO storefront_banners (
            id, tenant_id, banner_type, title, subtitle, image_url, action_text, action_url, sort_order, status, created_at, updated_at
        )
        VALUES
            ('dddd0001-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'HERO', 'Gear up. Rep your team.', 'Official merch. Exclusive styles.', 'https://images.unsplash.com/photo-1543326727-cf6c39e8f84c?q=80&w=2070&auto=format&fit=crop', 'SHOP NOW', '/collections/apparel', 0, 'ACTIVE', now(), now()),
            ('dddd0001-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'HERO', 'New Arrivals Are Here', 'Upgrade your game with the latest performance gear.', 'https://images.unsplash.com/photo-1518002171953-a080ee817e1f?q=80&w=2070&auto=format&fit=crop', 'EXPLORE NEW', '/collections/footwear', 1, 'ACTIVE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET title = EXCLUDED.title,
            subtitle = EXCLUDED.subtitle,
            image_url = EXCLUDED.image_url,
            status = 'ACTIVE',
            updated_at = now();

        -- 2. Category Images (Update existing seeded categories)
        UPDATE categories SET image_url = 'https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?q=80&w=1920&auto=format&fit=crop', updated_at = now() WHERE id = 'cccc0002-0001-4000-8000-000000000001'; -- Apparel
        UPDATE categories SET image_url = 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?q=80&w=1920&auto=format&fit=crop', updated_at = now() WHERE id = 'cccc0002-0002-4000-8000-000000000001'; -- Footwear
        UPDATE categories SET image_url = 'https://images.unsplash.com/photo-1601056586027-e92c57eb4ccb?q=80&w=1920&auto=format&fit=crop', updated_at = now() WHERE id = 'cccc0002-0003-4000-8000-000000000001'; -- Accessories
        UPDATE categories SET image_url = 'https://images.unsplash.com/photo-1518611012118-696072aa579a?q=80&w=1920&auto=format&fit=crop', updated_at = now() WHERE id = 'cccc0002-0004-4000-8000-000000000001'; -- Sports

        -- 3. Product Images (Insert for some seeded products)
        INSERT INTO product_images (
            id, tenant_id, product_id, image_storage_key, image_url, image_purpose, sort_order, is_primary_image, status, created_at, updated_at
        )
        VALUES
            ('dddd0002-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', 'jersey-1', 'https://images.unsplash.com/photo-1580087433295-ab2600c1030e?q=80&w=1000&auto=format&fit=crop', 'DEFAULT', 0, true, 'ACTIVE', now(), now()),
            ('dddd0002-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', 'jersey-2', 'https://images.unsplash.com/photo-1579952363873-27f3bade9f55?q=80&w=1000&auto=format&fit=crop', 'DEFAULT', 0, true, 'ACTIVE', now(), now()),
            ('dddd0002-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', 'shoe-1', 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?q=80&w=1000&auto=format&fit=crop', 'DEFAULT', 0, true, 'ACTIVE', now(), now()),
            ('dddd0002-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', 'shoe-2', 'https://images.unsplash.com/photo-1608231387042-66d1773070a5?q=80&w=1000&auto=format&fit=crop', 'DEFAULT', 0, true, 'ACTIVE', now(), now()),
            ('dddd0002-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0007-4000-8000-000000000001', 'cap-1', 'https://images.unsplash.com/photo-1588850561407-ed78c282e89b?q=80&w=1000&auto=format&fit=crop', 'DEFAULT', 0, true, 'ACTIVE', now(), now()),
            ('dddd0002-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', 'ball-1', 'https://images.unsplash.com/photo-1579952363873-27f3bade9f55?q=80&w=1000&auto=format&fit=crop', 'DEFAULT', 0, true, 'ACTIVE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET image_url = EXCLUDED.image_url,
            status = 'ACTIVE',
            updated_at = now();

        -- 4. Store Address (Insert for existing outlet)
        INSERT INTO outlet_addresses (
            id, tenant_id, outlet_id, address_type, address_line1, city, country_code, is_primary, status, created_at, updated_at
        )
        VALUES (
            'dddd0003-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            'PHYSICAL',
            '123 Sports Avenue',
            'Colombo',
            'LK',
            true,
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET address_line1 = EXCLUDED.address_line1,
            city = EXCLUDED.city,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM outlet_addresses
        WHERE id = 'dddd0003-0001-4000-8000-000000000001';

        DELETE FROM product_images
        WHERE id IN (
            'dddd0002-0001-4000-8000-000000000001',
            'dddd0002-0002-4000-8000-000000000001',
            'dddd0002-0003-4000-8000-000000000001',
            'dddd0002-0004-4000-8000-000000000001',
            'dddd0002-0005-4000-8000-000000000001',
            'dddd0002-0006-4000-8000-000000000001'
        );

        UPDATE categories SET image_url = NULL, updated_at = now() 
        WHERE id IN (
            'cccc0002-0001-4000-8000-000000000001',
            'cccc0002-0002-4000-8000-000000000001',
            'cccc0002-0003-4000-8000-000000000001',
            'cccc0002-0004-4000-8000-000000000001'
        );

        DELETE FROM storefront_banners
        WHERE id IN (
            'dddd0001-0001-4000-8000-000000000001',
            'dddd0001-0002-4000-8000-000000000001'
        );
        """;
}
