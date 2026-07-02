namespace E_POS.Infrastructure.Persistence.Seed;

public static class SubscriptionBillingCatalogSeedConstants
{
    public static readonly Guid CoreCommerceModuleId = Guid.Parse("71000000-0000-0000-0000-000000000001");
    public static readonly Guid OnlineStoreFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000001");
    public static readonly Guid ClickCollectFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000002");
    public static readonly Guid OfflineSyncFeatureId = Guid.Parse("72000000-0000-0000-0000-000000000003");
}

public static class SubscriptionBillingCatalogSeedData
{
    public const string UpSql = """
        INSERT INTO platform_modules (id, module_code, name, description, status, sort_order, created_at, updated_at)
        VALUES (
            '71000000-0000-0000-0000-000000000001',
            'core_commerce',
            'Core Commerce',
            'Core TM-EPOS commercial capabilities.',
            'ACTIVE',
            1,
            now(),
            now()
        )
        ON CONFLICT (module_code) DO UPDATE
        SET name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            updated_at = now();

        INSERT INTO platform_features (id, platform_module_id, feature_code, name, description, status, sort_order, created_at, updated_at)
        VALUES
            (
                '72000000-0000-0000-0000-000000000001',
                '71000000-0000-0000-0000-000000000001',
                'online_store',
                'Online Store',
                'Enable tenant online store channel.',
                'ACTIVE',
                1,
                now(),
                now()
            ),
            (
                '72000000-0000-0000-0000-000000000002',
                '71000000-0000-0000-0000-000000000001',
                'click_collect',
                'Click & Collect',
                'Enable click and collect ordering.',
                'ACTIVE',
                2,
                now(),
                now()
            ),
            (
                '72000000-0000-0000-0000-000000000003',
                '71000000-0000-0000-0000-000000000001',
                'offline_operation_sync',
                'Offline Operation Sync',
                'Enable offline operation synchronization.',
                'ACTIVE',
                3,
                now(),
                now()
            )
        ON CONFLICT (platform_module_id, feature_code) DO UPDATE
        SET name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM platform_features
        WHERE id IN (
            '72000000-0000-0000-0000-000000000001',
            '72000000-0000-0000-0000-000000000002',
            '72000000-0000-0000-0000-000000000003'
        );

        DELETE FROM platform_modules
        WHERE id = '71000000-0000-0000-0000-000000000001';
        """;
}
