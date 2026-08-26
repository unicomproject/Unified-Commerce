using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedStorefrontAzureMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                INSERT INTO media_assets (id, tenant_id, container_name, storage_key, original_file_name, mime_type, file_extension, file_size_bytes, checksum_hash, asset_type, asset_purpose, status, deletion_retry_count, created_at, updated_at)
                VALUES 
                ('eeee0001-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'images', 'storefront/promo-1.png', 'promo-1.png', 'image/png', '.png', 1024, '', 'IMAGE', 'STOREFRONT_PROMO_BANNER', 'ACTIVE', 0, NOW(), NOW()),
                ('eeee0001-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'images', 'storefront/promo-2.png', 'promo-2.png', 'image/png', '.png', 1024, '', 'IMAGE', 'STOREFRONT_PROMO_BANNER', 'ACTIVE', 0, NOW(), NOW()),
                ('eeee0001-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'images', 'storefront/hero-1.png', 'hero-1.png', 'image/png', '.png', 1024, '', 'IMAGE', 'STOREFRONT_HERO_BANNER', 'ACTIVE', 0, NOW(), NOW()),
                ('eeee0001-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'images', 'storefront/hero-2.jpg', 'hero-2.jpg', 'image/jpeg', '.jpg', 1024, '', 'IMAGE', 'STOREFRONT_HERO_BANNER', 'ACTIVE', 0, NOW(), NOW())
                ON CONFLICT (id) DO UPDATE
                SET storage_key = EXCLUDED.storage_key,
                    original_file_name = EXCLUDED.original_file_name,
                    mime_type = EXCLUDED.mime_type,
                    file_extension = EXCLUDED.file_extension,
                    asset_purpose = EXCLUDED.asset_purpose,
                    updated_at = NOW();

                UPDATE storefront_banners SET image_media_asset_id = 'eeee0001-0003-4000-8000-000000000001' WHERE id = 'dddd0001-0001-4000-8000-000000000001';
                UPDATE storefront_banners SET image_media_asset_id = 'eeee0001-0004-4000-8000-000000000001' WHERE id = 'dddd0001-0002-4000-8000-000000000001';
                UPDATE storefront_banners SET image_media_asset_id = 'eeee0001-0001-4000-8000-000000000001' WHERE id = 'dddd0001-0003-4000-8000-000000000001';
                UPDATE storefront_banners SET image_media_asset_id = 'eeee0001-0002-4000-8000-000000000001' WHERE id = 'dddd0001-0004-4000-8000-000000000001';
            ";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
