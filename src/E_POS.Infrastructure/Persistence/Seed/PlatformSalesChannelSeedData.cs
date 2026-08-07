namespace E_POS.Infrastructure.Persistence.Seed;

public static class PlatformSalesChannelSeedData
{
    public const string UpSql = """
        -- Seed Platform Sales Channels
        INSERT INTO platform_sales_channels (
            id, channel_code, default_name, channel_type, created_at, updated_at
        )
        VALUES
            ('d0000000-0000-4000-8000-000000000001', 'PHYSICAL', 'Physical Store', 'PHYSICAL', now(), now()),
            ('d0000000-0000-4000-8000-000000000002', 'ONLINE', 'E-Commerce', 'ONLINE', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET channel_code = EXCLUDED.channel_code,
            default_name = EXCLUDED.default_name,
            channel_type = EXCLUDED.channel_type,
            updated_at = now();

        -- Update existing POS sales channel from previous seeds (which now has empty GUID for platform_sales_channel_id)
        UPDATE sales_channels
        SET platform_sales_channel_id = 'd0000000-0000-4000-8000-000000000001',
            custom_name = 'Main POS',
            updated_at = now()
        WHERE id = 'bbbbbbbb-0006-4000-8000-000000000001';

        -- Seed E-Commerce Tenant Sales Channel
        INSERT INTO sales_channels (
            id, tenant_id, platform_sales_channel_id, custom_name,
            status, sort_order, created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-000b-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'd0000000-0000-4000-8000-000000000002',
            'E-Commerce Storefront',
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
        """;

    public const string CanonicalPosUpSql = """
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM platform_sales_channels
                WHERE (channel_type = 'POS' OR channel_code = 'POS')
                  AND id <> 'd0000000-0000-4000-8000-000000000003'
            ) THEN
                RAISE EXCEPTION 'Conflicting global POS platform sales channel exists.';
            END IF;

            INSERT INTO platform_sales_channels (
                id, channel_code, default_name, channel_type, created_at, updated_at
            )
            VALUES (
                'd0000000-0000-4000-8000-000000000003',
                'POS',
                'Point of Sale',
                'POS',
                now(),
                now()
            )
            ON CONFLICT (id) DO UPDATE
            SET channel_code = EXCLUDED.channel_code,
                default_name = EXCLUDED.default_name,
                channel_type = EXCLUDED.channel_type,
                updated_at = now();
        END $$;
        """;

    public const string DownSql = """
        DELETE FROM sales_channels
        WHERE id = 'bbbbbbbb-000b-4000-8000-000000000001';

        UPDATE sales_channels
        SET platform_sales_channel_id = '00000000-0000-0000-0000-000000000000',
            custom_name = 'POS'
        WHERE id = 'bbbbbbbb-0006-4000-8000-000000000001';

        DELETE FROM platform_sales_channels
        WHERE id IN (
            'd0000000-0000-4000-8000-000000000001',
            'd0000000-0000-4000-8000-000000000002',
            'd0000000-0000-4000-8000-000000000003',
            'd0000000-0000-4000-8000-000000000004',
            'd0000000-0000-4000-8000-000000000005'
        );
        """;
}
