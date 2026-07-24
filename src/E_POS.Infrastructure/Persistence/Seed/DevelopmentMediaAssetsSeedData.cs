namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentMediaAssetsSeedData
{
    public const string DevelopmentTenantId = "55555555-0000-4000-8000-000000000001";

    public const string UpSql = """
        WITH seed_media AS (
            SELECT
                md5('media_asset:product_images:' || source.id::text)::uuid AS id,
                source.tenant_id,
                'legacy-media' AS container_name,
                'legacy/product-images/' || source.id::text AS storage_key,
                left(source.source_url, 1000) AS public_url,
                left('seed-product-image-' || source.id::text || source.file_extension, 255) AS original_file_name,
                source.mime_type,
                source.file_extension,
                source.file_size_bytes,
                source.width_px,
                source.height_px,
                source.checksum_hash,
                'IMAGE' AS asset_type,
                source.asset_purpose,
                source.status,
                source.created_by_tenant_user_id,
                source.updated_by_tenant_user_id,
                source.created_at,
                source.updated_at
            FROM (
                SELECT
                    pi.id,
                    pi.tenant_id,
                    COALESCE(NULLIF(trim(pi.image_url), ''), NULLIF(trim(pi.image_storage_key), '')) AS source_url,
                    COALESCE(NULLIF(trim(pi.mime_type), ''), inferred.mime_type) AS mime_type,
                    inferred.file_extension,
                    GREATEST(COALESCE(pi.file_size_bytes, 1), 1) AS file_size_bytes,
                    CASE WHEN pi.width_px > 0 THEN pi.width_px ELSE NULL END AS width_px,
                    CASE WHEN pi.height_px > 0 THEN pi.height_px ELSE NULL END AS height_px,
                    COALESCE(NULLIF(trim(pi.checksum_hash), ''), md5('seed:product_images:' || pi.id::text || ':' || COALESCE(pi.image_url, pi.image_storage_key, ''))) AS checksum_hash,
                    left(COALESCE(NULLIF(upper(trim(pi.image_purpose)), ''), 'PRODUCT_IMAGE'), 80) AS asset_purpose,
                    CASE WHEN upper(trim(pi.status)) IN ('ACTIVE', 'INACTIVE', 'DELETED') THEN upper(trim(pi.status)) ELSE 'ACTIVE' END AS status,
                    pi.created_by_tenant_user_id,
                    pi.updated_by_tenant_user_id,
                    COALESCE(pi.created_at, now()) AS created_at,
                    COALESCE(pi.updated_at, pi.created_at, now()) AS updated_at
                FROM product_images pi
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                            ELSE 'image/jpeg'
                        END AS mime_type,
                        CASE
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                            WHEN lower(regexp_replace(COALESCE(pi.image_storage_key, pi.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpg'
                            ELSE '.jpg'
                        END AS file_extension
                ) inferred
                WHERE pi.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND (
                      pi.id::text LIKE 'cccc0008-%'
                      OR pi.id = 'cccc0026-0001-4000-8000-000000000001'
                      OR pi.id::text LIKE 'dddd0002-%'
                  )
            ) source
            WHERE source.source_url IS NOT NULL

            UNION ALL

            SELECT
                md5('media_asset:categories:' || source.id::text)::uuid,
                source.tenant_id,
                'legacy-media',
                'legacy/categories/' || source.id::text,
                left(source.source_url, 1000),
                left('seed-category-image-' || source.id::text || source.file_extension, 255),
                source.mime_type,
                source.file_extension,
                1,
                NULL,
                NULL,
                md5('seed:categories:' || source.id::text || ':' || source.source_url),
                'IMAGE',
                'CATEGORY',
                source.status,
                source.created_by_tenant_user_id,
                source.updated_by_tenant_user_id,
                source.created_at,
                source.updated_at
            FROM (
                SELECT
                    c.id,
                    c.tenant_id,
                    NULLIF(trim(c.image_url), '') AS source_url,
                    inferred.mime_type,
                    inferred.file_extension,
                    CASE WHEN upper(trim(c.status)) IN ('ACTIVE', 'INACTIVE', 'DELETED') THEN upper(trim(c.status)) ELSE 'ACTIVE' END AS status,
                    c.created_by_tenant_user_id,
                    c.updated_by_tenant_user_id,
                    COALESCE(c.created_at, now()) AS created_at,
                    COALESCE(c.updated_at, c.created_at, now()) AS updated_at
                FROM categories c
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                            ELSE 'image/jpeg'
                        END AS mime_type,
                        CASE
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                            WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpg'
                            ELSE '.jpg'
                        END AS file_extension
                ) inferred
                WHERE c.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND c.id::text LIKE 'cccc0002-%'
            ) source
            WHERE source.source_url IS NOT NULL

            UNION ALL

            SELECT
                md5('media_asset:brands:' || source.id::text)::uuid,
                source.tenant_id,
                'legacy-media',
                'legacy/brands/' || source.id::text,
                left(source.source_url, 1000),
                left('seed-brand-logo-' || source.id::text || source.file_extension, 255),
                source.mime_type,
                source.file_extension,
                1,
                NULL,
                NULL,
                md5('seed:brands:' || source.id::text || ':' || source.source_url),
                'IMAGE',
                'BRAND_LOGO',
                source.status,
                source.created_by_tenant_user_id,
                source.updated_by_tenant_user_id,
                source.created_at,
                source.updated_at
            FROM (
                SELECT
                    b.id,
                    b.tenant_id,
                    NULLIF(trim(b.logo_url), '') AS source_url,
                    inferred.mime_type,
                    inferred.file_extension,
                    CASE WHEN upper(trim(b.status)) IN ('ACTIVE', 'INACTIVE', 'DELETED') THEN upper(trim(b.status)) ELSE 'ACTIVE' END AS status,
                    b.created_by_tenant_user_id,
                    b.updated_by_tenant_user_id,
                    COALESCE(b.created_at, now()) AS created_at,
                    COALESCE(b.updated_at, b.created_at, now()) AS updated_at
                FROM brands b
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                            ELSE 'image/jpeg'
                        END AS mime_type,
                        CASE
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                            WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpg'
                            ELSE '.jpg'
                        END AS file_extension
                ) inferred
                WHERE b.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND b.id IN (
                      'cccccccc-0101-4000-8000-000000000001',
                      'cccccccc-0102-4000-8000-000000000001'
                  )
            ) source
            WHERE source.source_url IS NOT NULL

            UNION ALL

            SELECT
                md5('media_asset:product_option_values:' || source.id::text)::uuid,
                source.tenant_id,
                'legacy-media',
                'legacy/product-option-values/' || source.id::text,
                left(source.source_url, 1000),
                left('seed-product-option-value-image-' || source.id::text || source.file_extension, 255),
                source.mime_type,
                source.file_extension,
                1,
                NULL,
                NULL,
                md5('seed:product_option_values:' || source.id::text || ':' || source.source_url),
                'IMAGE',
                'PRODUCT_OPTION_VALUE',
                source.status,
                source.created_by_tenant_user_id,
                source.updated_by_tenant_user_id,
                source.created_at,
                source.updated_at
            FROM (
                SELECT
                    pov.id,
                    pov.tenant_id,
                    NULLIF(trim(pov.image_url), '') AS source_url,
                    inferred.mime_type,
                    inferred.file_extension,
                    CASE WHEN upper(trim(pov.status)) IN ('ACTIVE', 'INACTIVE', 'DELETED') THEN upper(trim(pov.status)) ELSE 'ACTIVE' END AS status,
                    pov.created_by_tenant_user_id,
                    pov.updated_by_tenant_user_id,
                    COALESCE(pov.created_at, now()) AS created_at,
                    COALESCE(pov.updated_at, pov.created_at, now()) AS updated_at
                FROM product_option_values pov
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                            ELSE 'image/jpeg'
                        END AS mime_type,
                        CASE
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                            WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpg'
                            ELSE '.jpg'
                        END AS file_extension
                ) inferred
                WHERE pov.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND (pov.id::text LIKE 'cccc0012-%' OR pov.id::text LIKE 'cccc0021-%')
            ) source
            WHERE source.source_url IS NOT NULL

            UNION ALL

            SELECT
                md5('media_asset:storefront_banners:' || source.id::text)::uuid,
                source.tenant_id,
                'legacy-media',
                'legacy/storefront-banners/' || source.id::text,
                left(source.source_url, 1000),
                left('seed-storefront-banner-image-' || source.id::text || source.file_extension, 255),
                source.mime_type,
                source.file_extension,
                1,
                NULL,
                NULL,
                md5('seed:storefront_banners:' || source.id::text || ':' || source.source_url),
                'IMAGE',
                'STOREFRONT_BANNER',
                source.status,
                NULL,
                NULL,
                source.created_at,
                source.updated_at
            FROM (
                SELECT
                    sb.id,
                    sb.tenant_id,
                    NULLIF(trim(sb.image_url), '') AS source_url,
                    inferred.mime_type,
                    inferred.file_extension,
                    CASE WHEN upper(trim(sb.status)) IN ('ACTIVE', 'INACTIVE', 'DELETED') THEN upper(trim(sb.status)) ELSE 'ACTIVE' END AS status,
                    COALESCE(sb.created_at, now()) AS created_at,
                    COALESCE(sb.updated_at, sb.created_at, now()) AS updated_at
                FROM storefront_banners sb
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                            ELSE 'image/jpeg'
                        END AS mime_type,
                        CASE
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                            WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpg'
                            ELSE '.jpg'
                        END AS file_extension
                ) inferred
                WHERE sb.tenant_id = '55555555-0000-4000-8000-000000000001'
                  AND sb.id::text LIKE 'dddd0001-%'
            ) source
            WHERE source.source_url IS NOT NULL

            UNION ALL

            SELECT
                md5('media_asset:tenant_profiles:' || source.id::text)::uuid,
                source.tenant_id,
                'legacy-media',
                'legacy/tenant-profiles/' || source.id::text,
                left(source.source_url, 1000),
                left('seed-tenant-logo-' || source.id::text || source.file_extension, 255),
                source.mime_type,
                source.file_extension,
                1,
                NULL,
                NULL,
                md5('seed:tenant_profiles:' || source.id::text || ':' || source.source_url),
                'IMAGE',
                'TENANT_LOGO',
                'ACTIVE',
                NULL,
                NULL,
                source.created_at,
                source.updated_at
            FROM (
                SELECT
                    tp.id,
                    tp.tenant_id,
                    NULLIF(trim(tp.logo_url), '') AS source_url,
                    inferred.mime_type,
                    inferred.file_extension,
                    COALESCE(tp.created_at, now()) AS created_at,
                    COALESCE(tp.updated_at, tp.created_at, now()) AS updated_at
                FROM tenant_profiles tp
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                            ELSE 'image/jpeg'
                        END AS mime_type,
                        CASE
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                            WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpg'
                            ELSE '.jpg'
                        END AS file_extension
                ) inferred
                WHERE tp.tenant_id = '55555555-0000-4000-8000-000000000001'
            ) source
            WHERE source.source_url IS NOT NULL
        )
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url, original_file_name,
            mime_type, file_extension, file_size_bytes, width_px, height_px, checksum_hash,
            asset_type, asset_purpose, status, created_by_tenant_user_id,
            updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            id, tenant_id, container_name, storage_key, public_url, original_file_name,
            mime_type, file_extension, file_size_bytes, width_px, height_px, checksum_hash,
            asset_type, asset_purpose, status, created_by_tenant_user_id,
            updated_by_tenant_user_id, created_at, updated_at
        FROM seed_media
        ON CONFLICT (tenant_id, container_name, storage_key) DO UPDATE
        SET public_url = EXCLUDED.public_url,
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
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();

        UPDATE product_images pi
        SET media_asset_id = md5('media_asset:product_images:' || pi.id::text)::uuid,
            updated_at = now()
        WHERE pi.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND (
              pi.id::text LIKE 'cccc0008-%'
              OR pi.id = 'cccc0026-0001-4000-8000-000000000001'
              OR pi.id::text LIKE 'dddd0002-%'
          )
          AND COALESCE(NULLIF(trim(pi.image_url), ''), NULLIF(trim(pi.image_storage_key), '')) IS NOT NULL
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = pi.tenant_id
                AND ma.id = md5('media_asset:product_images:' || pi.id::text)::uuid
          );

        UPDATE categories c
        SET image_media_asset_id = md5('media_asset:categories:' || c.id::text)::uuid,
            updated_at = now()
        WHERE c.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND c.id::text LIKE 'cccc0002-%'
          AND NULLIF(trim(c.image_url), '') IS NOT NULL
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = c.tenant_id
                AND ma.id = md5('media_asset:categories:' || c.id::text)::uuid
          );

        UPDATE brands b
        SET logo_media_asset_id = md5('media_asset:brands:' || b.id::text)::uuid,
            updated_at = now()
        WHERE b.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND b.id IN (
              'cccccccc-0101-4000-8000-000000000001',
              'cccccccc-0102-4000-8000-000000000001'
          )
          AND NULLIF(trim(b.logo_url), '') IS NOT NULL
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = b.tenant_id
                AND ma.id = md5('media_asset:brands:' || b.id::text)::uuid
          );

        UPDATE product_option_values pov
        SET image_media_asset_id = md5('media_asset:product_option_values:' || pov.id::text)::uuid,
            updated_at = now()
        WHERE pov.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND (pov.id::text LIKE 'cccc0012-%' OR pov.id::text LIKE 'cccc0021-%')
          AND NULLIF(trim(pov.image_url), '') IS NOT NULL
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = pov.tenant_id
                AND ma.id = md5('media_asset:product_option_values:' || pov.id::text)::uuid
          );

        UPDATE storefront_banners sb
        SET image_media_asset_id = md5('media_asset:storefront_banners:' || sb.id::text)::uuid,
            updated_at = now()
        WHERE sb.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND sb.id::text LIKE 'dddd0001-%'
          AND NULLIF(trim(sb.image_url), '') IS NOT NULL
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = sb.tenant_id
                AND ma.id = md5('media_asset:storefront_banners:' || sb.id::text)::uuid
          );

        UPDATE tenant_profiles tp
        SET logo_media_asset_id = md5('media_asset:tenant_profiles:' || tp.id::text)::uuid,
            updated_at = now()
        WHERE tp.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND NULLIF(trim(tp.logo_url), '') IS NOT NULL
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = tp.tenant_id
                AND ma.id = md5('media_asset:tenant_profiles:' || tp.id::text)::uuid
          );
        """;

    public const string DownSql = """
        UPDATE product_images pi
        SET media_asset_id = NULL,
            updated_at = now()
        WHERE pi.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND pi.media_asset_id = md5('media_asset:product_images:' || pi.id::text)::uuid
          AND (
              pi.id::text LIKE 'cccc0008-%'
              OR pi.id = 'cccc0026-0001-4000-8000-000000000001'
              OR pi.id::text LIKE 'dddd0002-%'
          );

        UPDATE categories c
        SET image_media_asset_id = NULL,
            updated_at = now()
        WHERE c.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND c.image_media_asset_id = md5('media_asset:categories:' || c.id::text)::uuid
          AND c.id::text LIKE 'cccc0002-%';

        UPDATE brands b
        SET logo_media_asset_id = NULL,
            updated_at = now()
        WHERE b.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND b.logo_media_asset_id = md5('media_asset:brands:' || b.id::text)::uuid
          AND b.id IN (
              'cccccccc-0101-4000-8000-000000000001',
              'cccccccc-0102-4000-8000-000000000001'
          );

        UPDATE product_option_values pov
        SET image_media_asset_id = NULL,
            updated_at = now()
        WHERE pov.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND pov.image_media_asset_id = md5('media_asset:product_option_values:' || pov.id::text)::uuid
          AND (pov.id::text LIKE 'cccc0012-%' OR pov.id::text LIKE 'cccc0021-%');

        UPDATE storefront_banners sb
        SET image_media_asset_id = NULL,
            updated_at = now()
        WHERE sb.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND sb.image_media_asset_id = md5('media_asset:storefront_banners:' || sb.id::text)::uuid
          AND sb.id::text LIKE 'dddd0001-%';

        UPDATE tenant_profiles tp
        SET logo_media_asset_id = NULL,
            updated_at = now()
        WHERE tp.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND tp.logo_media_asset_id = md5('media_asset:tenant_profiles:' || tp.id::text)::uuid;

        DELETE FROM media_assets ma
        WHERE ma.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND ma.container_name = 'legacy-media'
          AND (
              ma.id IN (
                  SELECT md5('media_asset:product_images:' || pi.id::text)::uuid
                  FROM product_images pi
                  WHERE pi.tenant_id = '55555555-0000-4000-8000-000000000001'
                    AND (
                        pi.id::text LIKE 'cccc0008-%'
                        OR pi.id = 'cccc0026-0001-4000-8000-000000000001'
                        OR pi.id::text LIKE 'dddd0002-%'
                    )
              )
              OR ma.id IN (
                  SELECT md5('media_asset:categories:' || c.id::text)::uuid
                  FROM categories c
                  WHERE c.tenant_id = '55555555-0000-4000-8000-000000000001'
                    AND c.id::text LIKE 'cccc0002-%'
              )
              OR ma.id IN (
                  SELECT md5('media_asset:brands:' || b.id::text)::uuid
                  FROM brands b
                  WHERE b.tenant_id = '55555555-0000-4000-8000-000000000001'
                    AND b.id IN (
                        'cccccccc-0101-4000-8000-000000000001',
                        'cccccccc-0102-4000-8000-000000000001'
                    )
              )
              OR ma.id IN (
                  SELECT md5('media_asset:product_option_values:' || pov.id::text)::uuid
                  FROM product_option_values pov
                  WHERE pov.tenant_id = '55555555-0000-4000-8000-000000000001'
                    AND (pov.id::text LIKE 'cccc0012-%' OR pov.id::text LIKE 'cccc0021-%')
              )
              OR ma.id IN (
                  SELECT md5('media_asset:storefront_banners:' || sb.id::text)::uuid
                  FROM storefront_banners sb
                  WHERE sb.tenant_id = '55555555-0000-4000-8000-000000000001'
                    AND sb.id::text LIKE 'dddd0001-%'
              )
              OR ma.id IN (
                  SELECT md5('media_asset:tenant_profiles:' || tp.id::text)::uuid
                  FROM tenant_profiles tp
                  WHERE tp.tenant_id = '55555555-0000-4000-8000-000000000001'
              )
          );
        """;
}