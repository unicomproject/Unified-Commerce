using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyMediaColumnsPhase4F : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Phase 4F final legacy media backfill.
                -- Converts any remaining legacy URL/storage rows created after the Phase 2 backfill
                -- before removing owner-level legacy image columns.
                WITH product_sources AS (
                    SELECT
                        pi.id AS owner_id,
                        pi.tenant_id,
                        md5('media_asset:phase4f:product_images:' || pi.id::text)::uuid AS media_asset_id,
                        COALESCE(NULLIF(trim(pi.image_url), ''), NULLIF(trim(pi.image_storage_key), '')) AS public_url,
                        COALESCE(NULLIF(trim(pi.image_storage_key), ''), 'legacy/product_images/' || pi.id::text) AS storage_key,
                        COALESCE(NULLIF(trim(pi.mime_type), ''),
                            CASE
                                WHEN lower(COALESCE(pi.image_url, pi.image_storage_key, '')) LIKE '%.png%' THEN 'image/png'
                                WHEN lower(COALESCE(pi.image_url, pi.image_storage_key, '')) LIKE '%.webp%' THEN 'image/webp'
                                ELSE 'image/jpeg'
                            END) AS mime_type,
                        CASE
                            WHEN lower(COALESCE(pi.image_url, pi.image_storage_key, '')) LIKE '%.png%' THEN '.png'
                            WHEN lower(COALESCE(pi.image_url, pi.image_storage_key, '')) LIKE '%.webp%' THEN '.webp'
                            ELSE '.jpg'
                        END AS file_extension,
                        COALESCE(pi.file_size_bytes, 1) AS file_size_bytes,
                        pi.width_px,
                        pi.height_px,
                        COALESCE(NULLIF(trim(pi.checksum_hash), ''), md5('legacy:product_images:' || pi.id::text)) AS checksum_hash
                    FROM product_images pi
                    WHERE pi.media_asset_id IS NULL
                      AND (
                          NULLIF(trim(pi.image_storage_key), '') IS NOT NULL OR
                          NULLIF(trim(pi.image_url), '') IS NOT NULL OR
                          NULLIF(trim(pi.mime_type), '') IS NOT NULL OR
                          pi.file_size_bytes IS NOT NULL OR
                          pi.width_px IS NOT NULL OR
                          pi.height_px IS NOT NULL OR
                          NULLIF(trim(pi.checksum_hash), '') IS NOT NULL
                      )
                )
                INSERT INTO media_assets (
                    id, tenant_id, container_name, storage_key, public_url,
                    original_file_name, mime_type, file_extension, file_size_bytes,
                    width_px, height_px, checksum_hash, asset_type, asset_purpose,
                    status, created_by_tenant_user_id, updated_by_tenant_user_id,
                    created_at, updated_at
                )
                SELECT
                    media_asset_id,
                    tenant_id,
                    'legacy-media',
                    storage_key,
                    public_url,
                    'legacy-image' || file_extension,
                    mime_type,
                    file_extension,
                    GREATEST(file_size_bytes, 1),
                    width_px,
                    height_px,
                    checksum_hash,
                    'IMAGE',
                    'PRODUCT',
                    'ACTIVE',
                    NULL,
                    NULL,
                    now(),
                    now()
                FROM product_sources
                ON CONFLICT (tenant_id, container_name, storage_key) DO UPDATE
                SET public_url = COALESCE(EXCLUDED.public_url, media_assets.public_url),
                    updated_at = now();

                WITH product_sources AS (
                    SELECT
                        pi.id AS owner_id,
                        pi.tenant_id,
                        md5('media_asset:phase4f:product_images:' || pi.id::text)::uuid AS media_asset_id,
                        COALESCE(NULLIF(trim(pi.image_storage_key), ''), 'legacy/product_images/' || pi.id::text) AS storage_key
                    FROM product_images pi
                    WHERE pi.media_asset_id IS NULL
                      AND (
                          NULLIF(trim(pi.image_storage_key), '') IS NOT NULL OR
                          NULLIF(trim(pi.image_url), '') IS NOT NULL OR
                          NULLIF(trim(pi.mime_type), '') IS NOT NULL OR
                          pi.file_size_bytes IS NOT NULL OR
                          pi.width_px IS NOT NULL OR
                          pi.height_px IS NOT NULL OR
                          NULLIF(trim(pi.checksum_hash), '') IS NOT NULL
                      )
                )
                UPDATE product_images pi
                SET media_asset_id = ma.id
                FROM product_sources source
                JOIN media_assets ma
                  ON ma.tenant_id = source.tenant_id
                 AND ma.container_name = 'legacy-media'
                 AND ma.storage_key = source.storage_key
                WHERE pi.id = source.owner_id
                  AND pi.tenant_id = source.tenant_id;

                WITH owner_sources AS (
                    SELECT 'categories' AS owner_table, c.id AS owner_id, c.tenant_id,
                           md5('media_asset:phase4f:categories:' || c.id::text)::uuid AS media_asset_id,
                           NULLIF(trim(c.image_url), '') AS public_url,
                           'legacy/categories/' || c.id::text AS storage_key,
                           'CATEGORY' AS asset_purpose
                    FROM categories c
                    WHERE c.image_media_asset_id IS NULL AND NULLIF(trim(c.image_url), '') IS NOT NULL
                    UNION ALL
                    SELECT 'brands', b.id, b.tenant_id,
                           md5('media_asset:phase4f:brands:' || b.id::text)::uuid,
                           NULLIF(trim(b.logo_url), ''),
                           'legacy/brands/' || b.id::text,
                           'BRAND_LOGO'
                    FROM brands b
                    WHERE b.logo_media_asset_id IS NULL AND NULLIF(trim(b.logo_url), '') IS NOT NULL
                    UNION ALL
                    SELECT 'product_option_values', pov.id, pov.tenant_id,
                           md5('media_asset:phase4f:product_option_values:' || pov.id::text)::uuid,
                           NULLIF(trim(pov.image_url), ''),
                           'legacy/product_option_values/' || pov.id::text,
                           'PRODUCT_OPTION_VALUE'
                    FROM product_option_values pov
                    WHERE pov.image_media_asset_id IS NULL AND NULLIF(trim(pov.image_url), '') IS NOT NULL
                    UNION ALL
                    SELECT 'storefront_banners', sb.id, sb.tenant_id,
                           md5('media_asset:phase4f:storefront_banners:' || sb.id::text)::uuid,
                           NULLIF(trim(sb.image_url), ''),
                           'legacy/storefront_banners/' || sb.id::text,
                           'STOREFRONT_BANNER'
                    FROM storefront_banners sb
                    WHERE sb.image_media_asset_id IS NULL AND NULLIF(trim(sb.image_url), '') IS NOT NULL
                    UNION ALL
                    SELECT 'tenant_profiles', tp.id, tp.tenant_id,
                           md5('media_asset:phase4f:tenant_profiles:' || tp.id::text)::uuid,
                           NULLIF(trim(tp.logo_url), ''),
                           'legacy/tenant_profiles/' || tp.id::text,
                           'TENANT_LOGO'
                    FROM tenant_profiles tp
                    WHERE tp.logo_media_asset_id IS NULL AND NULLIF(trim(tp.logo_url), '') IS NOT NULL
                )
                INSERT INTO media_assets (
                    id, tenant_id, container_name, storage_key, public_url,
                    original_file_name, mime_type, file_extension, file_size_bytes,
                    width_px, height_px, checksum_hash, asset_type, asset_purpose,
                    status, created_by_tenant_user_id, updated_by_tenant_user_id,
                    created_at, updated_at
                )
                SELECT
                    media_asset_id,
                    tenant_id,
                    'legacy-media',
                    storage_key,
                    public_url,
                    'legacy-image' || CASE
                        WHEN lower(public_url) LIKE '%.png%' THEN '.png'
                        WHEN lower(public_url) LIKE '%.webp%' THEN '.webp'
                        ELSE '.jpg'
                    END,
                    CASE
                        WHEN lower(public_url) LIKE '%.png%' THEN 'image/png'
                        WHEN lower(public_url) LIKE '%.webp%' THEN 'image/webp'
                        ELSE 'image/jpeg'
                    END,
                    CASE
                        WHEN lower(public_url) LIKE '%.png%' THEN '.png'
                        WHEN lower(public_url) LIKE '%.webp%' THEN '.webp'
                        ELSE '.jpg'
                    END,
                    1,
                    NULL,
                    NULL,
                    md5('legacy:' || owner_table || ':' || owner_id::text || ':' || public_url),
                    'IMAGE',
                    asset_purpose,
                    'ACTIVE',
                    NULL,
                    NULL,
                    now(),
                    now()
                FROM owner_sources
                ON CONFLICT (tenant_id, container_name, storage_key) DO UPDATE
                SET public_url = EXCLUDED.public_url,
                    updated_at = now();

                UPDATE categories c
                SET image_media_asset_id = ma.id
                FROM media_assets ma
                WHERE c.image_media_asset_id IS NULL
                  AND ma.tenant_id = c.tenant_id
                  AND ma.container_name = 'legacy-media'
                  AND ma.storage_key = 'legacy/categories/' || c.id::text;

                UPDATE brands b
                SET logo_media_asset_id = ma.id
                FROM media_assets ma
                WHERE b.logo_media_asset_id IS NULL
                  AND ma.tenant_id = b.tenant_id
                  AND ma.container_name = 'legacy-media'
                  AND ma.storage_key = 'legacy/brands/' || b.id::text;

                UPDATE product_option_values pov
                SET image_media_asset_id = ma.id
                FROM media_assets ma
                WHERE pov.image_media_asset_id IS NULL
                  AND ma.tenant_id = pov.tenant_id
                  AND ma.container_name = 'legacy-media'
                  AND ma.storage_key = 'legacy/product_option_values/' || pov.id::text;

                UPDATE storefront_banners sb
                SET image_media_asset_id = ma.id
                FROM media_assets ma
                WHERE sb.image_media_asset_id IS NULL
                  AND ma.tenant_id = sb.tenant_id
                  AND ma.container_name = 'legacy-media'
                  AND ma.storage_key = 'legacy/storefront_banners/' || sb.id::text;

                UPDATE tenant_profiles tp
                SET logo_media_asset_id = ma.id
                FROM media_assets ma
                WHERE tp.logo_media_asset_id IS NULL
                  AND ma.tenant_id = tp.tenant_id
                  AND ma.container_name = 'legacy-media'
                  AND ma.storage_key = 'legacy/tenant_profiles/' || tp.id::text;
                """);
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM product_images
                        WHERE media_asset_id IS NULL
                          AND (
                              NULLIF(trim(image_storage_key), '') IS NOT NULL OR
                              NULLIF(trim(image_url), '') IS NOT NULL OR
                              NULLIF(trim(mime_type), '') IS NOT NULL OR
                              file_size_bytes IS NOT NULL OR
                              width_px IS NOT NULL OR
                              height_px IS NOT NULL OR
                              NULLIF(trim(checksum_hash), '') IS NOT NULL
                          )
                    ) THEN
                        RAISE EXCEPTION 'Phase 4F blocked: product_images still has legacy image data without media_asset_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM categories
                        WHERE image_media_asset_id IS NULL
                          AND NULLIF(trim(image_url), '') IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Phase 4F blocked: categories still has legacy image_url without image_media_asset_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM brands
                        WHERE logo_media_asset_id IS NULL
                          AND NULLIF(trim(logo_url), '') IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Phase 4F blocked: brands still has legacy logo_url without logo_media_asset_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM product_option_values
                        WHERE image_media_asset_id IS NULL
                          AND NULLIF(trim(image_url), '') IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Phase 4F blocked: product_option_values still has legacy image_url without image_media_asset_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM storefront_banners
                        WHERE image_media_asset_id IS NULL
                          AND NULLIF(trim(image_url), '') IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Phase 4F blocked: storefront_banners still has legacy image_url without image_media_asset_id.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM tenant_profiles
                        WHERE logo_media_asset_id IS NULL
                          AND NULLIF(trim(logo_url), '') IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Phase 4F blocked: tenant_profiles still has legacy logo_url without logo_media_asset_id.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "tenant_profiles");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "storefront_banners");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "product_option_values");

            migrationBuilder.DropColumn(
                name: "checksum_hash",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "file_size_bytes",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "height_px",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "image_storage_key",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "mime_type",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "width_px",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "brands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "tenant_profiles",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "storefront_banners",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "product_option_values",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checksum_hash",
                table: "product_images",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "file_size_bytes",
                table: "product_images",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "height_px",
                table: "product_images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_storage_key",
                table: "product_images",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "product_images",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mime_type",
                table: "product_images",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "width_px",
                table: "product_images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "categories",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "brands",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE product_images AS pi
                SET image_storage_key = COALESCE(ma.storage_key, ''),
                    image_url = ma.public_url,
                    mime_type = ma.mime_type,
                    file_size_bytes = ma.file_size_bytes,
                    width_px = ma.width_px,
                    height_px = ma.height_px,
                    checksum_hash = ma.checksum_hash
                FROM media_assets AS ma
                WHERE pi.tenant_id = ma.tenant_id
                  AND pi.media_asset_id = ma.id;

                UPDATE categories AS c
                SET image_url = ma.public_url
                FROM media_assets AS ma
                WHERE c.tenant_id = ma.tenant_id
                  AND c.image_media_asset_id = ma.id;

                UPDATE brands AS b
                SET logo_url = ma.public_url
                FROM media_assets AS ma
                WHERE b.tenant_id = ma.tenant_id
                  AND b.logo_media_asset_id = ma.id;

                UPDATE product_option_values AS pov
                SET image_url = ma.public_url
                FROM media_assets AS ma
                WHERE pov.tenant_id = ma.tenant_id
                  AND pov.image_media_asset_id = ma.id;

                UPDATE storefront_banners AS sb
                SET image_url = COALESCE(ma.public_url, '')
                FROM media_assets AS ma
                WHERE sb.tenant_id = ma.tenant_id
                  AND sb.image_media_asset_id = ma.id;

                UPDATE tenant_profiles AS tp
                SET logo_url = ma.public_url
                FROM media_assets AS ma
                WHERE tp.tenant_id = ma.tenant_id
                  AND tp.logo_media_asset_id = ma.id;
                """);
        }
    }
}