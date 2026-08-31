namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;

public static class OneVerzeTenantSeedData
{
    // New Guids for the OneVerze tenant
    public static readonly Guid TenantId = Guid.Parse("08b0c8b0-a5bf-44f0-8814-cb2fe0120000");
    public static readonly Guid TenantProfileId = Guid.Parse("08b0c8b0-a5bf-44f0-8814-cb2fe0120001");

    public const string TenantCode = "ONEVERZE";
    public const string TenantSlug = "oneverze";
    public const string DisplayName = "OneVerze";

    public const string UpSql = """
        -- Ensure currency exists
        INSERT INTO currencies (
            id, currency_code, currency_name, currency_symbol,
            decimal_places, is_active, sort_order, created_at, updated_at
        )
        VALUES (
            '44444444-0001-4000-8000-000000000001',
            'LKR',
            'Sri Lankan Rupee',
            'Rs',
            2,
            true,
            1,
            now(),
            now()
        )
        ON CONFLICT (currency_code) DO UPDATE
        SET currency_name = EXCLUDED.currency_name,
            currency_symbol = EXCLUDED.currency_symbol,
            decimal_places = EXCLUDED.decimal_places,
            is_active = true,
            updated_at = now();

        -- Create the new OneVerze Tenant
        INSERT INTO tenants (
            id, tenant_code, tenant_slug, display_name, status,
            base_currency_code, default_timezone, default_locale,
            operating_mode, data_region, activated_at,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at
        )
        VALUES (
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            'ONEVERZE',
            'oneverze',
            'OneVerze',
            'active',
            'LKR',
            'Asia/Colombo',
            'en-LK',
            'unified_epos',
            'LK',
            now(),
            NULL,
            NULL,
            now(),
            now()
        )
        ON CONFLICT (tenant_code) DO UPDATE
        SET tenant_slug = EXCLUDED.tenant_slug,
            display_name = EXCLUDED.display_name,
            status = 'active',
            base_currency_code = EXCLUDED.base_currency_code,
            default_timezone = EXCLUDED.default_timezone,
            default_locale = EXCLUDED.default_locale,
            operating_mode = EXCLUDED.operating_mode,
            data_region = EXCLUDED.data_region,
            activated_at = COALESCE(tenants.activated_at, EXCLUDED.activated_at),
            updated_at = now();

        -- Create the Tenant Profile for OneVerze
        INSERT INTO tenant_profiles (
            id, tenant_id, business_type_id, legal_name, trading_name,
            primary_contact_name, primary_email, primary_phone,
            website_url, logo_media_asset_id, description,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at
        )
        VALUES (
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120001',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            NULL,
            'OneVerze',
            'OneVerze',
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            'Seeded tenant profile for the new OneVerze store.',
            NULL,
            NULL,
            now(),
            now()
        )
        ON CONFLICT (tenant_id) DO UPDATE
        SET legal_name = EXCLUDED.legal_name,
            trading_name = EXCLUDED.trading_name,
            description = EXCLUDED.description,
            updated_at = now();

        -- Enable Online Store and Click & Collect features for OneVerze
        INSERT INTO tenant_feature_entitlements (
            id, tenant_id, platform_feature_id, feature_id,
            entitlement_status, source_type, is_enabled,
            effective_from, effective_until, created_at, updated_at
        )
        VALUES
            (
                md5('08b0c8b0-a5bf-44f0-8814-cb2fe0120000:72000000-0000-0000-0000-000000000001')::uuid,
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                '72000000-0000-0000-0000-000000000001', -- online_store
                '72000000-0000-0000-0000-000000000001',
                'ENABLED',
                'MANUAL',
                true,
                now(),
                NULL,
                now(),
                now()
            ),
            (
                md5('08b0c8b0-a5bf-44f0-8814-cb2fe0120000:72000000-0000-0000-0000-000000000002')::uuid,
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                '72000000-0000-0000-0000-000000000002', -- click_collect
                '72000000-0000-0000-0000-000000000002',
                'ENABLED',
                'MANUAL',
                true,
                now(),
                NULL,
                now(),
                now()
            )
        ON CONFLICT (tenant_id, platform_feature_id) DO UPDATE
        SET entitlement_status = 'ENABLED',
            is_enabled = true,
            effective_until = NULL,
            updated_at = now();

        -- Hero Banners for OneVerze Storefront
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url, original_file_name,
            mime_type, file_extension, file_size_bytes, width_px, height_px, checksum_hash,
            asset_type, asset_purpose, status, created_at, updated_at
        )
        VALUES
            ('eeee0002-0001-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/storefront/hero-1.jpg', NULL, 'hero-1.jpg', 'image/jpeg', '.jpg', 500000, 1920, 1080, '', 'IMAGE', 'STOREFRONT_HERO_BANNER', 'ACTIVE', now(), now()),
            ('eeee0002-0002-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/storefront/hero-2.jpg', NULL, 'hero-2.jpg', 'image/jpeg', '.jpg', 500000, 1920, 1080, '', 'IMAGE', 'STOREFRONT_HERO_BANNER', 'ACTIVE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET container_name = 'images',
            storage_key = EXCLUDED.storage_key,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO storefront_banners (
            id, tenant_id, banner_type, title, subtitle, action_text, action_url,
            image_media_asset_id, sort_order, status, created_at, updated_at
        )
        VALUES
            (
                'dddd0002-0001-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'HERO',
                'GEAR UP. DOMINATE THE GAME.',
                NULL,
                'SHOP NOW',
                '/collections/match-jerseys',
                'eeee0002-0001-4000-8000-000000000001',
                0,
                'ACTIVE',
                now(),
                now()
            ),
            (
                'dddd0002-0002-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'HERO',
                'ENGINEERED FOR CHAMPIONS',
                NULL,
                'EXPLORE GEAR',
                '/collections/cricket-bats',
                'eeee0002-0002-4000-8000-000000000001',
                1,
                'ACTIVE',
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET title = EXCLUDED.title,
            subtitle = EXCLUDED.subtitle,
            action_text = EXCLUDED.action_text,
            action_url = EXCLUDED.action_url,
            image_media_asset_id = EXCLUDED.image_media_asset_id,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();

        -- Promo Banners for OneVerze Storefront
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url, original_file_name,
            mime_type, file_extension, file_size_bytes, width_px, height_px, checksum_hash,
            asset_type, asset_purpose, status, created_at, updated_at
        )
        VALUES
            ('eeee0003-0001-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/storefront/promo-bag.jpg', NULL, 'promo-bag.jpg', 'image/jpeg', '.jpg', 300000, 1024, 1024, '', 'IMAGE', 'STOREFRONT_PROMO_BANNER', 'ACTIVE', now(), now()),
            ('eeee0003-0002-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/storefront/promo-box.jpg', NULL, 'promo-box.jpg', 'image/jpeg', '.jpg', 300000, 1024, 1024, '', 'IMAGE', 'STOREFRONT_PROMO_BANNER', 'ACTIVE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET container_name = 'images',
            storage_key = EXCLUDED.storage_key,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO storefront_banners (
            id, tenant_id, banner_type, title, subtitle, action_text, action_url,
            image_media_asset_id, sort_order, status, created_at, updated_at
        )
        VALUES
            (
                'dddd0003-0001-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'PROMO',
                'Collect in as little as 30 mins',
                'CLICK & COLLECT',
                'SHOP NOW',
                '/collections/match-jerseys',
                'eeee0003-0001-4000-8000-000000000001',
                0,
                'ACTIVE',
                now(),
                now()
            ),
            (
                'dddd0003-0002-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'PROMO',
                'Free delivery on orders over Rs 5,000',
                'FREE DELIVERY',
                'SHOP NOW',
                '/collections/cricket-equipment',
                'eeee0003-0002-4000-8000-000000000001',
                1,
                'ACTIVE',
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET title = EXCLUDED.title,
                    subtitle = EXCLUDED.subtitle,
            action_text = EXCLUDED.action_text,
            action_url = EXCLUDED.action_url,
            image_media_asset_id = EXCLUDED.image_media_asset_id,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM tenant_profiles
        WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000';

        DELETE FROM tenants
        WHERE id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
           OR tenant_code = 'ONEVERZE'
           OR tenant_slug = 'oneverze';
        """;
}
