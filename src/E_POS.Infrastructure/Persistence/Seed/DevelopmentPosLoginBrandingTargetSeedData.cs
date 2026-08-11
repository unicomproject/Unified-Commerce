namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Deterministic, Development-only POS login branding data. The assets are
/// served by the API static-file pipeline and referenced through media_assets;
/// Flutter receives only the resolved public branding contract.
/// </summary>
public static class DevelopmentPosLoginBrandingTargetSeedData
{
    public const string UpSql = """
        WITH assets(
            id, storage_key, public_url, original_file_name, file_size_bytes,
            width_px, height_px, checksum_hash, asset_purpose) AS (
            VALUES
                ('dddddddd-0002-4000-8000-000000000001'::uuid,
                 'development-fixtures/pos-login/logo.png',
                 '/development-fixtures/pos-login/logo.png',
                 'oneverz-logo.png',
                 121929::bigint, 1254, 1254,
                 'a2c16c42147e4637a3f2117d468ddfc6d0c1f0d8759257f7889bd6271aa36360',
                 'TENANT_LOGO'),
                ('b1000000-0001-4000-8000-000000000001'::uuid,
                 'development-fixtures/pos-login/background.png',
                 '/development-fixtures/pos-login/background.png',
                 'pos-login-background.png',
                 1726462::bigint, 1086, 1448,
                 '61deb083325cf6bff2e2e824bfc549dd0e5e65e4825b628772bf8cb115882fe2',
                 'POS_LOGIN_BACKGROUND'),
                ('b1000000-0002-4000-8000-000000000001'::uuid,
                 'development-fixtures/pos-login/hero.png',
                 '/development-fixtures/pos-login/hero.png',
                 'pos-login-hero.png',
                 781939::bigint, 1536, 1024,
                 '63480a8b49360857345c0067629b4c104fcb634d16c84464290eccb3a6d8634e',
                 'POS_LOGIN_HERO')
        )
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url,
            original_file_name, mime_type, file_extension, file_size_bytes,
            width_px, height_px, checksum_hash, asset_type, asset_purpose,
            status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at)
        SELECT
            assets.id,
            '55555555-0000-4000-8000-000000000001'::uuid,
            'api-static-branding',
            assets.storage_key,
            assets.public_url,
            assets.original_file_name,
            'image/png',
            '.png',
            assets.file_size_bytes,
            assets.width_px,
            assets.height_px,
            assets.checksum_hash,
            'IMAGE',
            assets.asset_purpose,
            'ACTIVE',
            NULL,
            NULL,
            now(),
            now()
        FROM assets
        WHERE EXISTS (
            SELECT 1
            FROM tenants
            WHERE id = '55555555-0000-4000-8000-000000000001'::uuid)
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
            status = EXCLUDED.status,
            updated_at = now();

        INSERT INTO tenant_profiles (
            id, tenant_id, legal_name, trading_name, logo_media_asset_id,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at)
        SELECT
            'b3000000-0001-4000-8000-000000000001'::uuid,
            tenants.id,
            tenants.display_name,
            'OneVerz',
            'dddddddd-0002-4000-8000-000000000001'::uuid,
            NULL,
            NULL,
            now(),
            now()
        FROM tenants
        WHERE tenants.id = '55555555-0000-4000-8000-000000000001'::uuid
        ON CONFLICT (tenant_id) DO UPDATE
        SET trading_name = EXCLUDED.trading_name,
            logo_media_asset_id = EXCLUDED.logo_media_asset_id,
            updated_at = now();

        WITH values_to_seed(id, setting_key, setting_value) AS (
            VALUES
                ('b2000000-0001-4000-8000-000000000001'::uuid,
                 'pos.login.system_name',
                 'Smart Cashier System'),
                ('b2000000-0002-4000-8000-000000000001'::uuid,
                 'pos.login.description',
                 E'Powering every sale.\nEvery venue. Every day.'),
                ('b2000000-0003-4000-8000-000000000001'::uuid,
                 'pos.login.subtitle_template',
                 'Sign in to continue to {tenantName}'),
                ('b2000000-0004-4000-8000-000000000001'::uuid,
                 'pos.login.background_mode',
                 'IMAGE'),
                ('b2000000-0005-4000-8000-000000000001'::uuid,
                 'pos.login.background_color',
                 '#0D0F14'),
                ('b2000000-0006-4000-8000-000000000001'::uuid,
                 'pos.login.background_media_asset_id',
                 'b1000000-0001-4000-8000-000000000001'),
                ('b2000000-0007-4000-8000-000000000001'::uuid,
                 'pos.login.hero_media_asset_id',
                 'b1000000-0002-4000-8000-000000000001')
        )
        INSERT INTO tenant_settings (
            id, tenant_id, setting_definition_id, setting_value,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at)
        SELECT
            values_to_seed.id,
            '55555555-0000-4000-8000-000000000001'::uuid,
            definitions.id,
            to_jsonb(values_to_seed.setting_value::text),
            NULL,
            NULL,
            now(),
            now()
        FROM values_to_seed
        JOIN setting_definitions definitions
          ON definitions.setting_key = values_to_seed.setting_key
        WHERE definitions.status = 'ACTIVE'
          AND EXISTS (
              SELECT 1
              FROM tenants
              WHERE id = '55555555-0000-4000-8000-000000000001'::uuid)
        ON CONFLICT (tenant_id, setting_definition_id) DO UPDATE
        SET setting_value = EXCLUDED.setting_value,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM tenant_settings settings
        USING setting_definitions definitions
        WHERE settings.setting_definition_id = definitions.id
          AND settings.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND definitions.setting_key IN (
              'pos.login.system_name',
              'pos.login.description',
              'pos.login.subtitle_template',
              'pos.login.background_mode',
              'pos.login.background_color',
              'pos.login.background_media_asset_id',
              'pos.login.hero_media_asset_id');

        UPDATE tenant_profiles
        SET logo_media_asset_id = NULL,
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND logo_media_asset_id = 'dddddddd-0002-4000-8000-000000000001'::uuid;

        DELETE FROM media_assets
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
          AND id IN (
              'dddddddd-0002-4000-8000-000000000001'::uuid,
              'b1000000-0001-4000-8000-000000000001'::uuid,
              'b1000000-0002-4000-8000-000000000001'::uuid);
        """;
}
