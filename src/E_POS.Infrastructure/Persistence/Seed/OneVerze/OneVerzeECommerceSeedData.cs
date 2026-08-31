namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;

public static class OneVerzeECommerceSeedData
{
    public const string UpSql = """
        -- 1. Seed E-Commerce Tenant Sales Channel for OneVerze
        INSERT INTO sales_channels (
            id, tenant_id, platform_sales_channel_id, custom_name,
            status, sort_order, created_at, updated_at
        )
        VALUES (
            '55555555-0002-4000-8000-000000000002',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            'd0000000-0000-4000-8000-000000000002',
            'OneVerze',
            'ACTIVE',
            1,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET platform_sales_channel_id = EXCLUDED.platform_sales_channel_id,
            custom_name = EXCLUDED.custom_name,
            status = 'ACTIVE',
            updated_at = now();

        -- 2. Update Tenant Slug to oneverze
        UPDATE tenants 
        SET tenant_slug = 'oneverze', updated_at = now() 
        WHERE id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000';

        -- 3. Enable Online Store & Set Display Name
        INSERT INTO tenant_settings (
            id, tenant_id, setting_definition_id, setting_value,
            created_by_platform_user_id, updated_by_platform_user_id,
            created_at, updated_at
        )
        SELECT
            '55555555-0001-4000-8000-000000000001'::uuid,
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'::uuid,
            id,
            '{"setupEnabled": true, "storeStatus": "DRAFT", "businessDisplayName": "OneVerze", "storeSlug": "oneverze", "storeName": "OneVerze"}'::jsonb,
            NULL,
            NULL,
            now(),
            now()
        FROM setting_definitions
        WHERE setting_key = 'online_store.defaults'
        ON CONFLICT (tenant_id, setting_definition_id) DO UPDATE
        SET setting_value = EXCLUDED.setting_value,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM tenant_settings
        WHERE id = '55555555-0001-4000-8000-000000000001';

        UPDATE tenants 
        SET tenant_slug = NULL, updated_at = now() 
        WHERE id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000';

        DELETE FROM sales_channels
        WHERE id = '55555555-0002-4000-8000-000000000002';
        """;
}
