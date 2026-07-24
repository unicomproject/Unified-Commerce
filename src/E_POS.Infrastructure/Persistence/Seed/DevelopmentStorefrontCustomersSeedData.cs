namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentStorefrontCustomersSeedData
{
    public const string UpSql = """
        -- 1. Insert / Update 3 Development Storefront Customers
        INSERT INTO customers (
            id, tenant_id, customer_code, display_name, first_name, last_name, email, normalized_email, phone, normalized_phone, source_type, status, created_at, updated_at
        )
        VALUES
            (
                'e1111111-0001-4000-8000-000000000001',
                '55555555-0000-4000-8000-000000000001',
                'CUST-DEV-001',
                'Customer 1',
                'Customer',
                'One',
                'customer1@example.com',
                'CUSTOMER1@EXAMPLE.COM',
                '+15550000001',
                '+15550000001',
                'ECOMMERCE',
                'ACTIVE',
                now(),
                now()
            ),
            (
                'e1111111-0002-4000-8000-000000000002',
                '55555555-0000-4000-8000-000000000001',
                'CUST-DEV-002',
                'Customer 2',
                'Customer',
                'Two',
                'customer2@example.com',
                'CUSTOMER2@EXAMPLE.COM',
                '+15550000002',
                '+15550000002',
                'ECOMMERCE',
                'ACTIVE',
                now(),
                now()
            ),
            (
                'e1111111-0003-4000-8000-000000000003',
                '55555555-0000-4000-8000-000000000001',
                'CUST-DEV-003',
                'Customer 3',
                'Customer',
                'Three',
                'customer3@example.com',
                'CUSTOMER3@EXAMPLE.COM',
                '+15550000003',
                '+15550000003',
                'ECOMMERCE',
                'ACTIVE',
                now(),
                now()
            )
        ON CONFLICT (tenant_id, normalized_email) WHERE normalized_email IS NOT NULL AND status <> 'DELETED'
        DO UPDATE SET
            display_name = EXCLUDED.display_name,
            first_name = EXCLUDED.first_name,
            last_name = EXCLUDED.last_name,
            phone = EXCLUDED.phone,
            normalized_phone = EXCLUDED.normalized_phone,
            status = 'ACTIVE',
            updated_at = now();

        -- 2. Insert / Update Auth Accounts with Password: Admin@12345
        INSERT INTO customer_auth_accounts (
            id, tenant_id, customer_id, password_hash, failed_login_count, email_verified_at, status, created_at, updated_at
        )
        SELECT
            c.auth_id,
            '55555555-0000-4000-8000-000000000001'::uuid,
            cust.id,
            'PBKDF2-SHA256:100000:gg/mfMOXBS5PYc1Pu563tA==:sK9KInrLUc17SzdjICyBl1GvErK/wzQ6gLfTYO1pLNU=',
            0,
            now(),
            'ACTIVE',
            now(),
            now()
        FROM (
            VALUES
                ('e2222222-0001-4000-8000-000000000001'::uuid, 'CUSTOMER1@EXAMPLE.COM'),
                ('e2222222-0002-4000-8000-000000000002'::uuid, 'CUSTOMER2@EXAMPLE.COM'),
                ('e2222222-0003-4000-8000-000000000003'::uuid, 'CUSTOMER3@EXAMPLE.COM')
        ) AS c(auth_id, norm_email)
        JOIN customers cust ON cust.tenant_id = '55555555-0000-4000-8000-000000000001' AND cust.normalized_email = c.norm_email
        ON CONFLICT (tenant_id, customer_id) DO UPDATE
        SET password_hash = EXCLUDED.password_hash,
            failed_login_count = 0,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM customer_auth_accounts
        WHERE customer_id IN (
            SELECT id FROM customers WHERE tenant_id = '55555555-0000-4000-8000-000000000001' AND normalized_email IN ('CUSTOMER1@EXAMPLE.COM', 'CUSTOMER2@EXAMPLE.COM', 'CUSTOMER3@EXAMPLE.COM')
        );

        DELETE FROM customers
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001' AND normalized_email IN ('CUSTOMER1@EXAMPLE.COM', 'CUSTOMER2@EXAMPLE.COM', 'CUSTOMER3@EXAMPLE.COM');
        """;
}

