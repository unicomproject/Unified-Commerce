using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260723170000_AddMediaAssetsPhase2Backfill")]
    public partial class AddMediaAssetsPhase2Backfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO media_assets (
                    id,
                    tenant_id,
                    container_name,
                    storage_key,
                    public_url,
                    original_file_name,
                    mime_type,
                    file_extension,
                    file_size_bytes,
                    width_px,
                    height_px,
                    checksum_hash,
                    asset_type,
                    asset_purpose,
                    status,
                    created_by_tenant_user_id,
                    updated_by_tenant_user_id,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('media_asset:product_images:' || source.id::text)::uuid,
                    source.tenant_id,
                    'legacy-media',
                    'legacy/product-images/' || source.id::text,
                    source.source_url,
                    left('product-image-' || source.id::text || source.file_extension, 255),
                    source.mime_type,
                    source.file_extension,
                    source.file_size_bytes,
                    source.width_px,
                    source.height_px,
                    source.checksum_hash,
                    'IMAGE',
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
                        COALESCE(NULLIF(trim(pi.checksum_hash), ''), md5('legacy:product_images:' || pi.id::text || ':' || COALESCE(pi.image_url, pi.image_storage_key, ''))) AS checksum_hash,
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
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.png' THEN 'image/png'
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.webp' THEN 'image/webp'
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.jpg' THEN 'image/jpeg'
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN 'image/jpeg'
                                ELSE 'application/octet-stream'
                            END AS mime_type,
                            CASE
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                                WHEN lower(regexp_replace(COALESCE(pi.image_url, pi.image_storage_key, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpeg'
                                ELSE '.unknown'
                            END AS file_extension
                    ) inferred
                    WHERE pi.media_asset_id IS NULL
                ) source
                WHERE source.source_url IS NOT NULL
                ON CONFLICT (tenant_id, container_name, storage_key) DO NOTHING;

                UPDATE product_images pi
                SET media_asset_id = md5('media_asset:product_images:' || pi.id::text)::uuid
                WHERE pi.media_asset_id IS NULL
                  AND COALESCE(NULLIF(trim(pi.image_url), ''), NULLIF(trim(pi.image_storage_key), '')) IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM media_assets ma
                      WHERE ma.tenant_id = pi.tenant_id
                        AND ma.id = md5('media_asset:product_images:' || pi.id::text)::uuid
                  );
                """);

            migrationBuilder.Sql("""
                INSERT INTO media_assets (
                    id,
                    tenant_id,
                    container_name,
                    storage_key,
                    public_url,
                    original_file_name,
                    mime_type,
                    file_extension,
                    file_size_bytes,
                    width_px,
                    height_px,
                    checksum_hash,
                    asset_type,
                    asset_purpose,
                    status,
                    created_by_tenant_user_id,
                    updated_by_tenant_user_id,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('media_asset:categories:' || source.id::text)::uuid,
                    source.tenant_id,
                    'legacy-media',
                    'legacy/categories/' || source.id::text,
                    source.source_url,
                    left('category-image-' || source.id::text || source.file_extension, 255),
                    source.mime_type,
                    source.file_extension,
                    1,
                    NULL,
                    NULL,
                    md5('legacy:categories:' || source.id::text || ':' || source.source_url),
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
                                ELSE 'application/octet-stream'
                            END AS mime_type,
                            CASE
                                WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                                WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                                WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                                WHEN lower(regexp_replace(COALESCE(c.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpeg'
                                ELSE '.unknown'
                            END AS file_extension
                    ) inferred
                    WHERE c.image_media_asset_id IS NULL
                ) source
                WHERE source.source_url IS NOT NULL
                ON CONFLICT (tenant_id, container_name, storage_key) DO NOTHING;

                UPDATE categories c
                SET image_media_asset_id = md5('media_asset:categories:' || c.id::text)::uuid
                WHERE c.image_media_asset_id IS NULL
                  AND NULLIF(trim(c.image_url), '') IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM media_assets ma
                      WHERE ma.tenant_id = c.tenant_id
                        AND ma.id = md5('media_asset:categories:' || c.id::text)::uuid
                  );
                """);

            migrationBuilder.Sql("""
                INSERT INTO media_assets (
                    id,
                    tenant_id,
                    container_name,
                    storage_key,
                    public_url,
                    original_file_name,
                    mime_type,
                    file_extension,
                    file_size_bytes,
                    width_px,
                    height_px,
                    checksum_hash,
                    asset_type,
                    asset_purpose,
                    status,
                    created_by_tenant_user_id,
                    updated_by_tenant_user_id,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('media_asset:brands:' || source.id::text)::uuid,
                    source.tenant_id,
                    'legacy-media',
                    'legacy/brands/' || source.id::text,
                    source.source_url,
                    left('brand-logo-' || source.id::text || source.file_extension, 255),
                    source.mime_type,
                    source.file_extension,
                    1,
                    NULL,
                    NULL,
                    md5('legacy:brands:' || source.id::text || ':' || source.source_url),
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
                                ELSE 'application/octet-stream'
                            END AS mime_type,
                            CASE
                                WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                                WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                                WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                                WHEN lower(regexp_replace(COALESCE(b.logo_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpeg'
                                ELSE '.unknown'
                            END AS file_extension
                    ) inferred
                    WHERE b.logo_media_asset_id IS NULL
                ) source
                WHERE source.source_url IS NOT NULL
                ON CONFLICT (tenant_id, container_name, storage_key) DO NOTHING;

                UPDATE brands b
                SET logo_media_asset_id = md5('media_asset:brands:' || b.id::text)::uuid
                WHERE b.logo_media_asset_id IS NULL
                  AND NULLIF(trim(b.logo_url), '') IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM media_assets ma
                      WHERE ma.tenant_id = b.tenant_id
                        AND ma.id = md5('media_asset:brands:' || b.id::text)::uuid
                  );
                """);

            migrationBuilder.Sql("""
                INSERT INTO media_assets (
                    id,
                    tenant_id,
                    container_name,
                    storage_key,
                    public_url,
                    original_file_name,
                    mime_type,
                    file_extension,
                    file_size_bytes,
                    width_px,
                    height_px,
                    checksum_hash,
                    asset_type,
                    asset_purpose,
                    status,
                    created_by_tenant_user_id,
                    updated_by_tenant_user_id,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('media_asset:product_option_values:' || source.id::text)::uuid,
                    source.tenant_id,
                    'legacy-media',
                    'legacy/product-option-values/' || source.id::text,
                    source.source_url,
                    left('product-option-value-image-' || source.id::text || source.file_extension, 255),
                    source.mime_type,
                    source.file_extension,
                    1,
                    NULL,
                    NULL,
                    md5('legacy:product_option_values:' || source.id::text || ':' || source.source_url),
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
                                ELSE 'application/octet-stream'
                            END AS mime_type,
                            CASE
                                WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                                WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                                WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                                WHEN lower(regexp_replace(COALESCE(pov.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpeg'
                                ELSE '.unknown'
                            END AS file_extension
                    ) inferred
                    WHERE pov.image_media_asset_id IS NULL
                ) source
                WHERE source.source_url IS NOT NULL
                ON CONFLICT (tenant_id, container_name, storage_key) DO NOTHING;

                UPDATE product_option_values pov
                SET image_media_asset_id = md5('media_asset:product_option_values:' || pov.id::text)::uuid
                WHERE pov.image_media_asset_id IS NULL
                  AND NULLIF(trim(pov.image_url), '') IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM media_assets ma
                      WHERE ma.tenant_id = pov.tenant_id
                        AND ma.id = md5('media_asset:product_option_values:' || pov.id::text)::uuid
                  );
                """);

            migrationBuilder.Sql("""
                INSERT INTO media_assets (
                    id,
                    tenant_id,
                    container_name,
                    storage_key,
                    public_url,
                    original_file_name,
                    mime_type,
                    file_extension,
                    file_size_bytes,
                    width_px,
                    height_px,
                    checksum_hash,
                    asset_type,
                    asset_purpose,
                    status,
                    created_by_tenant_user_id,
                    updated_by_tenant_user_id,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('media_asset:storefront_banners:' || source.id::text)::uuid,
                    source.tenant_id,
                    'legacy-media',
                    'legacy/storefront-banners/' || source.id::text,
                    source.source_url,
                    left('storefront-banner-image-' || source.id::text || source.file_extension, 255),
                    source.mime_type,
                    source.file_extension,
                    1,
                    NULL,
                    NULL,
                    md5('legacy:storefront_banners:' || source.id::text || ':' || source.source_url),
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
                                ELSE 'application/octet-stream'
                            END AS mime_type,
                            CASE
                                WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                                WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                                WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                                WHEN lower(regexp_replace(COALESCE(sb.image_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpeg'
                                ELSE '.unknown'
                            END AS file_extension
                    ) inferred
                    WHERE sb.image_media_asset_id IS NULL
                ) source
                WHERE source.source_url IS NOT NULL
                ON CONFLICT (tenant_id, container_name, storage_key) DO NOTHING;

                UPDATE storefront_banners sb
                SET image_media_asset_id = md5('media_asset:storefront_banners:' || sb.id::text)::uuid
                WHERE sb.image_media_asset_id IS NULL
                  AND NULLIF(trim(sb.image_url), '') IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM media_assets ma
                      WHERE ma.tenant_id = sb.tenant_id
                        AND ma.id = md5('media_asset:storefront_banners:' || sb.id::text)::uuid
                  );
                """);

            migrationBuilder.Sql("""
                INSERT INTO media_assets (
                    id,
                    tenant_id,
                    container_name,
                    storage_key,
                    public_url,
                    original_file_name,
                    mime_type,
                    file_extension,
                    file_size_bytes,
                    width_px,
                    height_px,
                    checksum_hash,
                    asset_type,
                    asset_purpose,
                    status,
                    created_by_tenant_user_id,
                    updated_by_tenant_user_id,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('media_asset:tenant_profiles:' || source.id::text)::uuid,
                    source.tenant_id,
                    'legacy-media',
                    'legacy/tenant-profiles/' || source.id::text,
                    source.source_url,
                    left('tenant-logo-' || source.id::text || source.file_extension, 255),
                    source.mime_type,
                    source.file_extension,
                    1,
                    NULL,
                    NULL,
                    md5('legacy:tenant_profiles:' || source.id::text || ':' || source.source_url),
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
                                ELSE 'application/octet-stream'
                            END AS mime_type,
                            CASE
                                WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.png' THEN '.png'
                                WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.webp' THEN '.webp'
                                WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.jpg' THEN '.jpg'
                                WHEN lower(regexp_replace(COALESCE(tp.logo_url, ''), '[?#].*$', '')) LIKE '%.jpeg' THEN '.jpeg'
                                ELSE '.unknown'
                            END AS file_extension
                    ) inferred
                    WHERE tp.logo_media_asset_id IS NULL
                ) source
                WHERE source.source_url IS NOT NULL
                ON CONFLICT (tenant_id, container_name, storage_key) DO NOTHING;

                UPDATE tenant_profiles tp
                SET logo_media_asset_id = md5('media_asset:tenant_profiles:' || tp.id::text)::uuid
                WHERE tp.logo_media_asset_id IS NULL
                  AND NULLIF(trim(tp.logo_url), '') IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM media_assets ma
                      WHERE ma.tenant_id = tp.tenant_id
                        AND ma.id = md5('media_asset:tenant_profiles:' || tp.id::text)::uuid
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE product_images
                SET media_asset_id = NULL
                WHERE media_asset_id = md5('media_asset:product_images:' || id::text)::uuid;

                UPDATE categories
                SET image_media_asset_id = NULL
                WHERE image_media_asset_id = md5('media_asset:categories:' || id::text)::uuid;

                UPDATE brands
                SET logo_media_asset_id = NULL
                WHERE logo_media_asset_id = md5('media_asset:brands:' || id::text)::uuid;

                UPDATE product_option_values
                SET image_media_asset_id = NULL
                WHERE image_media_asset_id = md5('media_asset:product_option_values:' || id::text)::uuid;

                UPDATE storefront_banners
                SET image_media_asset_id = NULL
                WHERE image_media_asset_id = md5('media_asset:storefront_banners:' || id::text)::uuid;

                UPDATE tenant_profiles
                SET logo_media_asset_id = NULL
                WHERE logo_media_asset_id = md5('media_asset:tenant_profiles:' || id::text)::uuid;

                DELETE FROM media_assets
                WHERE container_name = 'legacy-media'
                  AND (
                      storage_key LIKE 'legacy/product-images/%'
                      OR storage_key LIKE 'legacy/categories/%'
                      OR storage_key LIKE 'legacy/brands/%'
                      OR storage_key LIKE 'legacy/product-option-values/%'
                      OR storage_key LIKE 'legacy/storefront-banners/%'
                      OR storage_key LIKE 'legacy/tenant-profiles/%'
                  );
                """);
        }
    }
}
