namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentCheckoutPrerequisitesSeedData
{
    public const string UpSql = """
        INSERT INTO sales_channels (
            id, tenant_id, channel_code, channel_name, channel_type, channel_mode,
            status, sort_order, created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0006-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'POS',
            'POS',
            'POS',
            'OFFLINE',
            'ACTIVE',
            0,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET channel_name = EXCLUDED.channel_name,
            channel_type = EXCLUDED.channel_type,
            channel_mode = EXCLUDED.channel_mode,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO payment_methods (
            id, tenant_id, method_code, method_name, method_type,
            is_active_for_pos, is_active_for_online, requires_manual_confirmation,
            supports_refund, requires_reference, allows_change, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            ('bbbbbbbb-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CASH', 'Cash', 'CASH', true, false, false, true, false, true, 0, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('bbbbbbbb-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CARD', 'Card', 'CARD', true, false, false, true, false, false, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('bbbbbbbb-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'QR', 'QR', 'QR', true, false, false, true, false, false, 2, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('bbbbbbbb-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'SPLIT', 'Split', 'SPLIT', true, false, false, true, false, true, 3, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET method_name = EXCLUDED.method_name,
            is_active_for_pos = true,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM payment_methods
        WHERE id IN (
            'bbbbbbbb-0007-4000-8000-000000000001',
            'bbbbbbbb-0008-4000-8000-000000000001',
            'bbbbbbbb-0009-4000-8000-000000000001',
            'bbbbbbbb-000a-4000-8000-000000000001'
        );

        DELETE FROM sales_channels
        WHERE id = 'bbbbbbbb-0006-4000-8000-000000000001';
        """;
}
