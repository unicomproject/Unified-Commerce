namespace E_POS.Infrastructure.Persistence.Seed;

public static class PlatformModuleCatalogPrerequisiteSeedData
{
    public const string UpSql = """
        INSERT INTO platform_modules (
            id,
            module_code,
            name,
            description,
            status,
            sort_order,
            module_key,
            module_name,
            is_core_module,
            created_at,
            updated_at)
        VALUES
            (
                '71500000-0000-0000-0000-000000000001',
                'user_management',
                'User Management',
                NULL,
                'ACTIVE',
                3,
                'user_management',
                'User Management',
                true,
                now(),
                now()
            ),
            (
                '71500000-0000-0000-0000-000000000002',
                'outlet_till_core',
                'Outlet & Till Core',
                NULL,
                'ACTIVE',
                9,
                'outlet_till_core',
                'Outlet & Till Core',
                true,
                now(),
                now()
            )
        ON CONFLICT (module_code) DO UPDATE
        SET name = EXCLUDED.name,
            module_key = EXCLUDED.module_key,
            module_name = EXCLUDED.module_name,
            is_core_module = true,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            updated_at = now();

        INSERT INTO platform_features (
            id,
            platform_module_id,
            feature_code,
            name,
            description,
            status,
            sort_order,
            feature_key,
            feature_name,
            is_core_feature,
            created_at,
            updated_at)
        SELECT
            '72500000-0000-0000-0000-000000000001',
            pm.id,
            'user_accounts',
            'User Accounts',
            NULL,
            'ACTIVE',
            1,
            'user_accounts',
            'User Accounts',
            true,
            now(),
            now()
        FROM platform_modules pm
        WHERE pm.module_code = 'user_management'
        ON CONFLICT (feature_key) DO UPDATE
        SET platform_module_id = EXCLUDED.platform_module_id,
            feature_code = EXCLUDED.feature_code,
            name = EXCLUDED.name,
            feature_name = EXCLUDED.feature_name,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            is_core_feature = true,
            updated_at = now();

        INSERT INTO platform_features (
            id,
            platform_module_id,
            feature_code,
            name,
            description,
            status,
            sort_order,
            feature_key,
            feature_name,
            is_core_feature,
            created_at,
            updated_at)
        SELECT
            '72500000-0000-0000-0000-000000000002',
            pm.id,
            'outlet_management',
            'Outlet Management',
            NULL,
            'ACTIVE',
            1,
            'outlet_management',
            'Outlet Management',
            true,
            now(),
            now()
        FROM platform_modules pm
        WHERE pm.module_code = 'outlet_till_core'
        ON CONFLICT (feature_key) DO UPDATE
        SET platform_module_id = EXCLUDED.platform_module_id,
            feature_code = EXCLUDED.feature_code,
            name = EXCLUDED.name,
            feature_name = EXCLUDED.feature_name,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            is_core_feature = true,
            updated_at = now();

        INSERT INTO platform_features (
            id,
            platform_module_id,
            feature_code,
            name,
            description,
            status,
            sort_order,
            feature_key,
            feature_name,
            is_core_feature,
            created_at,
            updated_at)
        SELECT
            '72500000-0000-0000-0000-000000000003',
            pm.id,
            'till_management',
            'Till Management',
            NULL,
            'ACTIVE',
            2,
            'till_management',
            'Till Management',
            true,
            now(),
            now()
        FROM platform_modules pm
        WHERE pm.module_code = 'outlet_till_core'
        ON CONFLICT (feature_key) DO UPDATE
        SET platform_module_id = EXCLUDED.platform_module_id,
            feature_code = EXCLUDED.feature_code,
            name = EXCLUDED.name,
            feature_name = EXCLUDED.feature_name,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            is_core_feature = true,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM platform_features
        WHERE id IN (
            '72500000-0000-0000-0000-000000000001',
            '72500000-0000-0000-0000-000000000002',
            '72500000-0000-0000-0000-000000000003'
        );

        DELETE FROM platform_modules
        WHERE id IN (
            '71500000-0000-0000-0000-000000000001',
            '71500000-0000-0000-0000-000000000002'
        )
          AND NOT EXISTS (
              SELECT 1 FROM platform_features pf
              WHERE pf.platform_module_id = platform_modules.id
          );
        """;
}
