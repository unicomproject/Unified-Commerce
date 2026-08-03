namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentCashierProfileImageSeedData
{
    public static readonly Guid ProfileImageAssetId =
        Guid.Parse("dddddddd-0001-4000-8000-000000000001");

    public const string ProfileImageUrl =
        "https://imgcdn.stablediffusionweb.com/2024/10/15/12d6f588-c9ab-4c05-82f0-99f9c2c0453f.jpg";

    public const string UpSql = """
        INSERT INTO media_assets (
            id, tenant_id, container_name, storage_key, public_url,
            original_file_name, mime_type, file_extension, file_size_bytes,
            width_px, height_px, checksum_hash, asset_type, asset_purpose,
            status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        SELECT
            'dddddddd-0001-4000-8000-000000000001',
            users.tenant_id,
            'external-profile-images',
            'development/users/cashier001/profile.jpg',
            'https://imgcdn.stablediffusionweb.com/2024/10/15/12d6f588-c9ab-4c05-82f0-99f9c2c0453f.jpg',
            'cashier001-profile.jpg',
            'image/jpeg',
            '.jpg',
            1,
            NULL,
            NULL,
            md5('https://imgcdn.stablediffusionweb.com/2024/10/15/12d6f588-c9ab-4c05-82f0-99f9c2c0453f.jpg'),
            'IMAGE',
            'USER_PROFILE',
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
            checksum_hash = EXCLUDED.checksum_hash,
            asset_type = EXCLUDED.asset_type,
            asset_purpose = EXCLUDED.asset_purpose,
            status = 'ACTIVE',
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();

        UPDATE tenant_users
        SET profile_image_url = 'dddddddd-0001-4000-8000-000000000001',
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND email = 'CASHIER001@GMAIL.COM';
        """;

    public const string DownSql = """
        UPDATE tenant_users
        SET profile_image_url = NULL,
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND email = 'CASHIER001@GMAIL.COM'
          AND profile_image_url = 'dddddddd-0001-4000-8000-000000000001';

        DELETE FROM media_assets
        WHERE id = 'dddddddd-0001-4000-8000-000000000001'
          AND tenant_id = '55555555-0000-4000-8000-000000000001';
        """;
}
