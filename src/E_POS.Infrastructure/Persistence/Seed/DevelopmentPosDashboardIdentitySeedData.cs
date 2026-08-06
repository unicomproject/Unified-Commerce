namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosDashboardIdentitySeedData
{
    public static readonly Guid BrandingLogoAssetId =
        Guid.Parse("dddddddd-0002-4000-8000-000000000001");

    public const string BrandingLogoPublicUrl = "/branding/oneverz-pos-bag.png";

    public const string UpSql = """
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url,
            original_file_name, mime_type, file_extension, file_size_bytes,
            width_px, height_px, checksum_hash, asset_type, asset_purpose,
            status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        SELECT
            'dddddddd-0002-4000-8000-000000000001',
            users.tenant_id,
            'api-static-branding',
            'branding/oneverz-pos-bag.png',
            '/branding/oneverz-pos-bag.png',
            'oneverz-pos-bag.png',
            'image/png',
            '.png',
            1,
            1254,
            1254,
            md5('/branding/oneverz-pos-bag.png'),
            'IMAGE',
            'TENANT_LOGO',
            'ACTIVE',
            users.id,
            users.id,
            now(),
            now()
        FROM tenant_users users
        WHERE users.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND users.email = 'CASHIER001@GMAIL.COM'
        ON CONFLICT (id) DO UPDATE
        SET public_url = EXCLUDED.public_url,
            original_file_name = EXCLUDED.original_file_name,
            mime_type = EXCLUDED.mime_type,
            file_extension = EXCLUDED.file_extension,
            width_px = EXCLUDED.width_px,
            height_px = EXCLUDED.height_px,
            checksum_hash = EXCLUDED.checksum_hash,
            asset_type = EXCLUDED.asset_type,
            asset_purpose = EXCLUDED.asset_purpose,
            status = 'ACTIVE',
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();

        UPDATE tenant_profiles
        SET logo_media_asset_id = 'dddddddd-0002-4000-8000-000000000001',
            trading_name = 'OneVerz POS',
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001';

        UPDATE tenant_users
        SET full_name = 'Kavin',
            display_name = 'Kavin',
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND email = 'CASHIER001@GMAIL.COM';
        """;

    public const string DownSql = """
        UPDATE tenant_profiles
        SET logo_media_asset_id = NULL,
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND logo_media_asset_id = 'dddddddd-0002-4000-8000-000000000001';

        DELETE FROM media_assets
        WHERE id = 'dddddddd-0002-4000-8000-000000000001'
          AND tenant_id = '55555555-0000-4000-8000-000000000001';
        """;
}
