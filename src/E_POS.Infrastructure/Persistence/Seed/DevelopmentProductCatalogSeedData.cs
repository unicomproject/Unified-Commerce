namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentProductCatalogSeedData
{
    public const string UpSql = """
        INSERT INTO departments (
            id,
            tenant_id,
            department_code,
            department_name,
            description,
            sort_order,
            status,
            created_by_tenant_user_id,
            updated_by_tenant_user_id,
            created_at,
            updated_at
        )
        VALUES (
            'cccccccc-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'GENERAL',
            'General Merchandise',
            'Development seed department for product catalog lookups.',
            1,
            'ACTIVE',
            '99999999-0001-4000-8000-000000000001',
            '99999999-0001-4000-8000-000000000001',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET department_name = EXCLUDED.department_name,
            description = EXCLUDED.description,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO categories (
            id,
            tenant_id,
            department_id,
            parent_category_id,
            category_code,
            category_name,
            category_slug,
            description,
            sort_order,
            status,
            created_by_tenant_user_id,
            updated_by_tenant_user_id,
            created_at,
            updated_at
        )
        VALUES
            (
                'cccccccc-0002-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccccccc-0001-4000-8000-000000000001',
                NULL,
                'GROCERIES',
                'Groceries',
                'groceries',
                'Development seed parent category.',
                1,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            ),
            (
                'cccccccc-0003-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccccccc-0001-4000-8000-000000000001',
                NULL,
                'ELECTRONICS',
                'Electronics',
                'electronics',
                'Development seed parent category.',
                2,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            ),
            (
                'cccccccc-0011-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccccccc-0001-4000-8000-000000000001',
                'cccccccc-0002-4000-8000-000000000001',
                'BEVERAGES',
                'Beverages',
                'beverages',
                'Development seed sub category.',
                1,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            ),
            (
                'cccccccc-0012-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccccccc-0001-4000-8000-000000000001',
                'cccccccc-0002-4000-8000-000000000001',
                'SNACKS',
                'Snacks',
                'snacks',
                'Development seed sub category.',
                2,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            ),
            (
                'cccccccc-0021-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'cccccccc-0001-4000-8000-000000000001',
                'cccccccc-0003-4000-8000-000000000001',
                'MOBILE_PHONES',
                'Mobile Phones',
                'mobile-phones',
                'Development seed sub category.',
                1,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET department_id = EXCLUDED.department_id,
            parent_category_id = EXCLUDED.parent_category_id,
            category_name = EXCLUDED.category_name,
            category_slug = EXCLUDED.category_slug,
            description = EXCLUDED.description,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO brands (
            id,
            tenant_id,
            brand_code,
            brand_name,
            brand_slug,
            description,
            logo_url,
            status,
            created_by_tenant_user_id,
            updated_by_tenant_user_id,
            created_at,
            updated_at
        )
        VALUES
            (
                'cccccccc-0101-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'UNILEVER',
                'Unilever',
                'unilever',
                'Development seed brand.',
                NULL,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            ),
            (
                'cccccccc-0102-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'SAMSUNG',
                'Samsung',
                'samsung',
                'Development seed brand.',
                NULL,
                'ACTIVE',
                '99999999-0001-4000-8000-000000000001',
                '99999999-0001-4000-8000-000000000001',
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET brand_name = EXCLUDED.brand_name,
            brand_slug = EXCLUDED.brand_slug,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM categories
        WHERE id IN (
            'cccccccc-0011-4000-8000-000000000001',
            'cccccccc-0012-4000-8000-000000000001',
            'cccccccc-0021-4000-8000-000000000001',
            'cccccccc-0002-4000-8000-000000000001',
            'cccccccc-0003-4000-8000-000000000001'
        );

        DELETE FROM brands
        WHERE id IN (
            'cccccccc-0101-4000-8000-000000000001',
            'cccccccc-0102-4000-8000-000000000001'
        );

        DELETE FROM departments
        WHERE id = 'cccccccc-0001-4000-8000-000000000001';
        """;
}
