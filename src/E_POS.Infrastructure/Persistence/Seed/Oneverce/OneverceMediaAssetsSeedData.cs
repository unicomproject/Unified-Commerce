namespace E_POS.Infrastructure.Persistence.Seed.Oneverce;

public static class OneverceMediaAssetsSeedData
{
    // Uses the TenantId from OneverceTenantSeedData
    public static readonly Guid TenantId = Guid.Parse("07fdfd9f-33a2-46e5-9af0-99acf219fd57");

    // Replace these with your actual Asset and Product IDs when you add data
    public static readonly Guid SampleMediaAssetId = Guid.Parse("aaaa0001-0000-4000-8000-000000000001");
    public static readonly Guid SampleProductId = Guid.Parse("bbbb0001-0000-4000-8000-000000000001");

    public static readonly string UpSql = $"""
        -- INSERT INTO media_assets 
        -- (id, tenant_id, container_name, storage_key, original_file_name, mime_type, file_extension, file_size_bytes, asset_type, asset_purpose, status, created_at, updated_at)
        -- VALUES 
        -- ('{SampleMediaAssetId}', '{TenantId}', 'images', 'tenants/{TenantId}/products/{SampleProductId}/images/{SampleMediaAssetId}.jpg', 'seed-product.jpg', 'image/jpeg', '.jpg', 2048, 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', NOW(), NOW());

        -- INSERT INTO product_images 
        -- (id, tenant_id, product_id, media_asset_id, image_purpose, sort_order, is_primary_image, status, created_at, updated_at)
        -- VALUES
        -- (gen_random_uuid(), '{TenantId}', '{SampleProductId}', '{SampleMediaAssetId}', 'PRODUCT_IMAGE', 0, true, 'ACTIVE', NOW(), NOW());
        """;

    public static readonly string DownSql = $"""
        -- DELETE FROM product_images WHERE media_asset_id = '{SampleMediaAssetId}';
        -- DELETE FROM media_assets WHERE id = '{SampleMediaAssetId}';
        """;
}
