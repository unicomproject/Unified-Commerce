namespace E_POS.Infrastructure.Persistence.Seed;

public static class OneverceTenantSeedData
{
    public static readonly Guid TenantId = Guid.Parse("07fdfd9f-33a2-46e5-9af0-99acf219fd57");
    public static readonly Guid TenantProfileId = Guid.Parse("6cc4d7a2-a5bf-44f0-8814-cb2fe012099f");

    public const string TenantCode = "ONEVERCE";
    public const string TenantSlug = "oneverce";
    public const string DisplayName = "Oneverce";

    public const string UpSql = """
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

        INSERT INTO tenants (
            id, tenant_code, tenant_slug, display_name, status,
            base_currency_code, default_timezone, default_locale,
            operating_mode, data_region, activated_at,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at
        )
        VALUES (
            '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
            'ONEVERCE',
            'oneverce',
            'Oneverce',
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

        INSERT INTO tenant_profiles (
            id, tenant_id, business_type_id, legal_name, trading_name,
            primary_contact_name, primary_email, primary_phone,
            website_url, logo_media_asset_id, description,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at
        )
        VALUES (
            '6cc4d7a2-a5bf-44f0-8814-cb2fe012099f',
            '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
            NULL,
            'Oneverce',
            'Oneverce',
            NULL,
            NULL,
            NULL,
            NULL,
            NULL,
            'Seeded tenant profile for Oneverce.',
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
        """;

    public const string DownSql = """
        DELETE FROM tenant_profiles
        WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57';

        DELETE FROM tenants
        WHERE id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
           OR tenant_code = 'ONEVERCE'
           OR tenant_slug = 'oneverce';
        """;
}
