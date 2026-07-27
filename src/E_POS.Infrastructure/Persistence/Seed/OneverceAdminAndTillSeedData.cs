namespace E_POS.Infrastructure.Persistence.Seed;

public static class OneverceAdminAndTillSeedData
{
    public static readonly Guid AdminUserId = Guid.Parse("32547b85-5066-4c3c-8100-818841f74ce2");
    public static readonly Guid TenantAdminRoleId = Guid.Parse("170f6cbb-342b-4974-87ab-e1b727561020");
    public static readonly Guid TenantAdminUserRoleId = Guid.Parse("d88b4347-7b7b-49f8-8f8f-275503039cd4");

    public static readonly Guid MainOutletTillId = Guid.Parse("c443916a-8111-458f-97f9-d60bcc139a9f");
    public static readonly Guid OutletTwoTillId = Guid.Parse("c92ac999-f593-4c1f-9b78-9886f475628a");
    public static readonly Guid OutletThreeTillId = Guid.Parse("ff026f1c-ff63-46b2-ac4d-58fc99b1cf76");
    public static readonly Guid OutletFourTillId = Guid.Parse("6faff19c-73ea-4777-9182-01d5fcf140c8");
    public static readonly Guid OutletFiveTillId = Guid.Parse("ae46d8f9-6f7d-48eb-9309-948090620d8a");

    public const string AdminEmail = "ADMIN@ONEVERCE.LK";
    public const string AdminPasswordHash =
        "PBKDF2-SHA256:100000:yDeM0dWO6UHyEV0xR2al5Q==:/50BRK/3tG0okJDw/9iFX9AK/IrEXLovBaxRutSpNqw=";

    public const string UpSql = """
        INSERT INTO tenant_roles (
            id, tenant_id, source_role_template_id, source_role_template_version_id,
            role_code, role_name, role_description, is_custom, is_active,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES (
            '170f6cbb-342b-4974-87ab-e1b727561020',
            '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
            '66666666-0000-4000-8000-000000000001',
            '66666666-0001-4000-8000-000000000001',
            'TENANT_ADMIN',
            'Tenant Admin',
            'Oneverce tenant administrator.',
            FALSE,
            TRUE,
            NULL,
            NULL,
            now(),
            now()
        )
        ON CONFLICT (tenant_id, role_code) DO UPDATE
        SET role_name = EXCLUDED.role_name,
            role_description = EXCLUDED.role_description,
            is_custom = EXCLUDED.is_custom,
            is_active = TRUE,
            updated_at = now();

        INSERT INTO tenant_users (
            id, tenant_id, email, encrypted_password, phone, unmasked_phone,
            password_salt, full_name, display_name, outlet_id, default_outlet_id,
            user_type, account_status, failed_login_attempts, accepted_privacy_terms,
            accepted_terms_version, source_user_type, notes, created_at, updated_at
        )
        VALUES (
            '32547b85-5066-4c3c-8100-818841f74ce2',
            '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
            'ADMIN@ONEVERCE.LK',
            'PBKDF2-SHA256:100000:yDeM0dWO6UHyEV0xR2al5Q==:/50BRK/3tG0okJDw/9iFX9AK/IrEXLovBaxRutSpNqw=',
            NULL,
            NULL,
            'pbkdf2_embedded',
            'Oneverce Tenant Admin',
            'Oneverce Admin',
            'd4e295a3-2556-4081-87ce-7ebe4db9e276',
            'ONEV-001',
            'admin',
            'ACTIVE',
            0,
            true,
            '1.0',
            'admin',
            'Oneverce seeded tenant admin user.',
            now(),
            now()
        )
        ON CONFLICT (tenant_id, email) DO UPDATE
        SET encrypted_password = EXCLUDED.encrypted_password,
            password_salt = EXCLUDED.password_salt,
            full_name = EXCLUDED.full_name,
            display_name = EXCLUDED.display_name,
            outlet_id = EXCLUDED.outlet_id,
            default_outlet_id = EXCLUDED.default_outlet_id,
            user_type = EXCLUDED.user_type,
            account_status = 'ACTIVE',
            failed_login_attempts = 0,
            accepted_privacy_terms = true,
            accepted_terms_version = EXCLUDED.accepted_terms_version,
            source_user_type = EXCLUDED.source_user_type,
            notes = EXCLUDED.notes,
            updated_at = now();

        INSERT INTO tenant_user_roles (
            id, tenant_id, user_id, role_id, assigned_by_tenant_user_id,
            assigned_at, revoked_at, created_at
        )
        SELECT
            'd88b4347-7b7b-49f8-8f8f-275503039cd4',
            '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
            users.id,
            roles.id,
            NULL,
            now(),
            NULL,
            now()
        FROM tenant_users users
        JOIN tenant_roles roles
          ON roles.tenant_id = users.tenant_id
         AND roles.role_code = 'TENANT_ADMIN'
        WHERE users.tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
          AND users.email = 'ADMIN@ONEVERCE.LK'
        ON CONFLICT (tenant_id, user_id, role_id) DO UPDATE
        SET revoked_at = NULL,
            assigned_at = COALESCE(tenant_user_roles.assigned_at, EXCLUDED.assigned_at);

        INSERT INTO tenant_role_permissions (
            id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
            revoked_by_tenant_user_id, granted_at, revoked_at, notes, created_at
        )
        SELECT
            md5('oneverce:tenant_admin:' || permission_definitions.permission_code)::uuid,
            '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
            roles.id,
            permission_definitions.id,
            users.id,
            NULL,
            now(),
            NULL,
            'Oneverce tenant admin permission seed.',
            now()
        FROM tenant_roles roles
        JOIN tenant_users users
          ON users.tenant_id = roles.tenant_id
         AND users.email = 'ADMIN@ONEVERCE.LK'
        CROSS JOIN permission_definitions
        WHERE roles.tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
          AND roles.role_code = 'TENANT_ADMIN'
          AND permission_definitions.is_active = true
        ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
        SET revoked_at = NULL,
            revoked_by_tenant_user_id = NULL,
            notes = EXCLUDED.notes;

        INSERT INTO tills (
            id, tenant_id, outlet_id, till_area_name, till_number, till_name, till_code,
            till_type, default_opening_float_amount, currency_code, is_cash_managed, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            (
                'c443916a-8111-458f-97f9-d60bcc139a9f',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                'd4e295a3-2556-4081-87ce-7ebe4db9e276',
                'Front',
                1,
                'Oneverce Main Outlet Till 01',
                'ONEV-001-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                now(),
                now()
            ),
            (
                'c92ac999-f593-4c1f-9b78-9886f475628a',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                '316892a5-e193-4edf-97cd-b6d1d1c6b86c',
                'Front',
                1,
                'Oneverce Outlet 02 Till 01',
                'ONEV-002-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                now(),
                now()
            ),
            (
                'ff026f1c-ff63-46b2-ac4d-58fc99b1cf76',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                '63ec79d4-6d56-48e8-ad95-8bd0276cfbd7',
                'Front',
                1,
                'Oneverce Outlet 03 Till 01',
                'ONEV-003-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                now(),
                now()
            ),
            (
                '6faff19c-73ea-4777-9182-01d5fcf140c8',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                '66aff51a-2b04-48b7-a860-60553b46ba53',
                'Front',
                1,
                'Oneverce Outlet 04 Till 01',
                'ONEV-004-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                now(),
                now()
            ),
            (
                'ae46d8f9-6f7d-48eb-9309-948090620d8a',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                '933e657f-b0a6-425c-a6da-31e0bc5acaac',
                'Front',
                1,
                'Oneverce Outlet 05 Till 01',
                'ONEV-005-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57' AND email = 'ADMIN@ONEVERCE.LK' LIMIT 1),
                now(),
                now()
            )
        ON CONFLICT (tenant_id, outlet_id, till_code) DO UPDATE
        SET till_area_name = EXCLUDED.till_area_name,
            till_number = EXCLUDED.till_number,
            till_name = EXCLUDED.till_name,
            till_type = EXCLUDED.till_type,
            default_opening_float_amount = EXCLUDED.default_opening_float_amount,
            currency_code = EXCLUDED.currency_code,
            is_cash_managed = EXCLUDED.is_cash_managed,
            status = 'ACTIVE',
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM tills
        WHERE id IN (
            'c443916a-8111-458f-97f9-d60bcc139a9f',
            'c92ac999-f593-4c1f-9b78-9886f475628a',
            'ff026f1c-ff63-46b2-ac4d-58fc99b1cf76',
            '6faff19c-73ea-4777-9182-01d5fcf140c8',
            'ae46d8f9-6f7d-48eb-9309-948090620d8a'
        );

        DELETE FROM tenant_role_permissions
        WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
          AND role_id IN (
              SELECT id FROM tenant_roles
              WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
                AND role_code = 'TENANT_ADMIN'
          );

        DELETE FROM tenant_user_roles
        WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
          AND user_id IN (
              SELECT id FROM tenant_users
              WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
                AND email = 'ADMIN@ONEVERCE.LK'
          );

        DELETE FROM tenant_roles
        WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
          AND role_code = 'TENANT_ADMIN';

        DELETE FROM tenant_users
        WHERE tenant_id = '07fdfd9f-33a2-46e5-9af0-99acf219fd57'
          AND email = 'ADMIN@ONEVERCE.LK';
        """;
}
