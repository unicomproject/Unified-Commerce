namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosLoginBrandingSeedData
{
    public static readonly Guid BackgroundAssetId =
        Guid.Parse("b1000000-0001-4000-8000-000000000001");
    public static readonly Guid HeroAssetId =
        Guid.Parse("b1000000-0002-4000-8000-000000000001");
    public static readonly Guid CrossTenantBackgroundAssetId =
        Guid.Parse("b1000000-0003-4000-8000-000000000001");
    public static readonly Guid CrossTenantHeroAssetId =
        Guid.Parse("b1000000-0004-4000-8000-000000000001");

    public const string UpSql = """
        UPDATE tenants
        SET tenant_slug = 'arenasports',
            updated_at = now()
        WHERE id = '55555555-0000-4000-8000-000000000001'
          AND (tenant_slug IS NULL OR trim(tenant_slug) = '');

        WITH fixtures(id, tenant_id, storage_key, public_url, original_file_name, asset_purpose) AS (
            VALUES
                ('b1000000-0001-4000-8000-000000000001'::uuid,
                 '55555555-0000-4000-8000-000000000001'::uuid,
                 'development/pos-login/background.png',
                 '/development-fixtures/pos-login/background.png',
                 'pos-login-background.png',
                 'POS_LOGIN_BACKGROUND'),
                ('b1000000-0002-4000-8000-000000000001'::uuid,
                 '55555555-0000-4000-8000-000000000001'::uuid,
                 'development/pos-login/hero.png',
                 '/development-fixtures/pos-login/hero.png',
                 'pos-login-hero.png',
                 'POS_LOGIN_HERO'),
                ('b1000000-0003-4000-8000-000000000001'::uuid,
                 (SELECT id FROM tenants WHERE tenant_slug = 'oneverce' LIMIT 1),
                 'development/cross-tenant/pos-login/background.png',
                 '/development-fixtures/cross-tenant/pos-login/background.png',
                 'cross-tenant-pos-login-background.png',
                 'POS_LOGIN_BACKGROUND'),
                ('b1000000-0004-4000-8000-000000000001'::uuid,
                 (SELECT id FROM tenants WHERE tenant_slug = 'oneverce' LIMIT 1),
                 'development/cross-tenant/pos-login/hero.png',
                 '/development-fixtures/cross-tenant/pos-login/hero.png',
                 'cross-tenant-pos-login-hero.png',
                 'POS_LOGIN_HERO')
        )
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url,
            original_file_name, mime_type, file_extension, file_size_bytes,
            width_px, height_px, checksum_hash, asset_type, asset_purpose,
            status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at)
        SELECT
            fixtures.id,
            fixtures.tenant_id,
            'development-fixtures',
            fixtures.storage_key,
            fixtures.public_url,
            fixtures.original_file_name,
            'image/png',
            '.png',
            1,
            1600,
            900,
            md5(fixtures.storage_key),
            'IMAGE',
            fixtures.asset_purpose,
            'ACTIVE',
            NULL,
            NULL,
            now(),
            now()
        FROM fixtures
        WHERE EXISTS (SELECT 1 FROM tenants WHERE tenants.id = fixtures.tenant_id)
        ON CONFLICT (id) DO UPDATE
        SET tenant_id = EXCLUDED.tenant_id,
            container_name = EXCLUDED.container_name,
            storage_key = EXCLUDED.storage_key,
            public_url = EXCLUDED.public_url,
            original_file_name = EXCLUDED.original_file_name,
            mime_type = EXCLUDED.mime_type,
            file_extension = EXCLUDED.file_extension,
            file_size_bytes = EXCLUDED.file_size_bytes,
            width_px = EXCLUDED.width_px,
            height_px = EXCLUDED.height_px,
            checksum_hash = EXCLUDED.checksum_hash,
            asset_type = EXCLUDED.asset_type,
            asset_purpose = EXCLUDED.asset_purpose,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM media_assets
        WHERE id IN (
            'b1000000-0001-4000-8000-000000000001',
            'b1000000-0002-4000-8000-000000000001',
            'b1000000-0003-4000-8000-000000000001',
            'b1000000-0004-4000-8000-000000000001');
        """;
}
