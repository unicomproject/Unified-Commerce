


namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;

public static class OneVerzeCategorySeedData
{
    public const string UpSql = """
        -- 1. Insert Department
        INSERT INTO departments (
            id, tenant_id, department_code, department_name, description, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES (
            '66666666-0001-4000-8000-000000000001',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            'CRICKET',
            'Cricket Merchandise',
            'All cricket related merchandise and equipment.',
            0,
            'ACTIVE',
            NULL,
            NULL,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET department_name = EXCLUDED.department_name,
            status = 'ACTIVE',
            updated_at = now();

        -- 2. Insert Media Assets for Categories
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url, original_file_name,
            mime_type, file_extension, file_size_bytes, width_px, height_px, checksum_hash,
            asset_type, asset_purpose, status, created_at, updated_at
        )
        VALUES
            ('77777777-0001-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_equipment.jpg', NULL, 'cricket_equipment.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0002-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_apparel.jpg', NULL, 'cricket_apparel.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0003-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_bats.jpg', NULL, 'cricket_bats.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0004-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_jerseys.jpg', NULL, 'cricket_jerseys.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0005-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_footwear.jpg', NULL, 'cricket_footwear.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0006-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_accessories.jpg', NULL, 'cricket_accessories.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0007-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/training_gear.jpg', NULL, 'training_gear.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0008-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_shoes.jpg', NULL, 'cricket_shoes.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0009-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_kit_bags.jpg', NULL, 'cricket_kit_bags.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now()),
            ('77777777-0010-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/categories/cricket_practice_nets.jpg', NULL, 'cricket_practice_nets.jpg', 'image/jpeg', '.jpg', 10240, 1024, 1024, '', 'IMAGE', 'CATEGORY_IMAGE', 'ACTIVE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET storage_key = EXCLUDED.storage_key,
            public_url = EXCLUDED.public_url,
            status = 'ACTIVE';

        -- 3. Insert Categories and Subcategories
        INSERT INTO categories (
            id, tenant_id, department_id, parent_category_id, category_code, category_name,
            category_slug, description, image_media_asset_id, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            -- Main Category 1 (Equipment)
            ('66666666-0002-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', NULL, 'EQUIPMENT', 'Cricket Equipment', 'cricket-equipment', 'Bats, balls, and gear.', '77777777-0001-4000-8000-000000000001', 0, 'ACTIVE', NULL, NULL, now(), now()),
            -- Main Category 2 (Apparel)
            ('66666666-0003-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', NULL, 'APPAREL', 'Team Apparel', 'team-apparel', 'Jerseys and clothing.', '77777777-0002-4000-8000-000000000001', 1, 'ACTIVE', NULL, NULL, now(), now()),
            -- Main Category 3 (Footwear)
            ('66666666-0006-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', NULL, 'FOOTWEAR', 'Footwear', 'cricket-footwear', 'High-performance cricket spikes and shoes.', '77777777-0005-4000-8000-000000000001', 2, 'ACTIVE', NULL, NULL, now(), now()),
            -- Main Category 4 (Accessories)
            ('66666666-0007-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', NULL, 'ACCESSORIES', 'Accessories', 'cricket-accessories', 'Gloves, pads, and grips.', '77777777-0006-4000-8000-000000000001', 3, 'ACTIVE', NULL, NULL, now(), now()),
            -- Main Category 5 (Training Gear)
            ('66666666-0008-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', NULL, 'TRAINING_GEAR', 'Training Gear', 'training-gear', 'Cones, ladders, and balls.', '77777777-0007-4000-8000-000000000001', 4, 'ACTIVE', NULL, NULL, now(), now()),

            -- Subcategory 1 (Under Equipment)
            ('66666666-0004-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', '66666666-0002-4000-8000-000000000001', 'BATS', 'Cricket Bats', 'cricket-bats', 'English and Kashmir willow bats.', '77777777-0003-4000-8000-000000000001', 0, 'ACTIVE', NULL, NULL, now(), now()),
            -- Subcategory 2 (Under Apparel)
            ('66666666-0005-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', '66666666-0003-4000-8000-000000000001', 'JERSEYS', 'Match Jerseys', 'match-jerseys', 'Official team match jerseys.', '77777777-0004-4000-8000-000000000001', 0, 'ACTIVE', NULL, NULL, now(), now()),
            -- Subcategory 3 (Under Footwear)
            ('66666666-0009-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', '66666666-0006-4000-8000-000000000001', 'SHOES', 'Cricket Shoes', 'cricket-shoes', 'Shoes with spikes.', '77777777-0008-4000-8000-000000000001', 0, 'ACTIVE', NULL, NULL, now(), now()),
            -- Subcategory 4 (Under Accessories)
            ('66666666-0010-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', '66666666-0007-4000-8000-000000000001', 'KIT_BAGS', 'Kit Bags', 'kit-bags', 'Large cricket kit bags.', '77777777-0009-4000-8000-000000000001', 0, 'ACTIVE', NULL, NULL, now(), now()),
            -- Subcategory 5 (Under Training Gear)
            ('66666666-0011-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '66666666-0001-4000-8000-000000000001', '66666666-0008-4000-8000-000000000001', 'NETS', 'Practice Nets', 'practice-nets', 'Portable practice nets.', '77777777-0010-4000-8000-000000000001', 0, 'ACTIVE', NULL, NULL, now(), now())
        ON CONFLICT (id) DO UPDATE
        SET category_name = EXCLUDED.category_name,
            image_media_asset_id = EXCLUDED.image_media_asset_id,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM categories
        WHERE id IN (
            '66666666-0002-4000-8000-000000000001',
            '66666666-0003-4000-8000-000000000001',
            '66666666-0004-4000-8000-000000000001',
            '66666666-0005-4000-8000-000000000001',
            '66666666-0006-4000-8000-000000000001',
            '66666666-0007-4000-8000-000000000001',
            '66666666-0008-4000-8000-000000000001',
            '66666666-0009-4000-8000-000000000001',
            '66666666-0010-4000-8000-000000000001',
            '66666666-0011-4000-8000-000000000001'
        );

        DELETE FROM media_assets
        WHERE id IN (
            '77777777-0001-4000-8000-000000000001',
            '77777777-0002-4000-8000-000000000001',
            '77777777-0003-4000-8000-000000000001',
            '77777777-0004-4000-8000-000000000001',
            '77777777-0005-4000-8000-000000000001',
            '77777777-0006-4000-8000-000000000001',
            '77777777-0007-4000-8000-000000000001',
            '77777777-0008-4000-8000-000000000001',
            '77777777-0009-4000-8000-000000000001',
            '77777777-0010-4000-8000-000000000001'
        );

        DELETE FROM departments
        WHERE id = '66666666-0001-4000-8000-000000000001';
        """;
}
