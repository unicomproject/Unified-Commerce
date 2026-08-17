namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;

public static class OneVerzeAdminAndTillSeedData
{
    public static readonly Guid AdminUserId = Guid.Parse("33333333-0001-4000-8000-000000000001");
    public static readonly Guid TenantAdminRoleId = Guid.Parse("33333333-0002-4000-8000-000000000002");
    public static readonly Guid TenantAdminUserRoleId = Guid.Parse("33333333-0003-4000-8000-000000000003");

    public static readonly Guid MainOutletTillId = Guid.Parse("44444444-0001-4000-8000-000000000001");
    public static readonly Guid OutletTwoTillId = Guid.Parse("44444444-0002-4000-8000-000000000002");
    public static readonly Guid OutletThreeTillId = Guid.Parse("44444444-0003-4000-8000-000000000003");

    public const string AdminEmail = "ADMIN@ONEVERZE.LK";
    
    // Default seeded password hash for "Password123!"
    public const string AdminPasswordHash =
        "PBKDF2-SHA256:100000:MyGLqisYhNcDqD5C4894Zg==:EzY4vyzeVpsHUIjG0eBEi9mpSdNv6p2+nFTBBBt5ujc=";

    public const string UpSql = """
        INSERT INTO tenant_roles (
            id, tenant_id, source_role_template_id, source_role_template_version_id,
            role_code, role_name, role_description, is_custom, is_active,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES (
            '33333333-0002-4000-8000-000000000002',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            '66666666-0000-4000-8000-000000000001',
            '66666666-0001-4000-8000-000000000001',
            'TENANT_ADMIN',
            'Tenant Admin',
            'OneVerze tenant administrator.',
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
            accepted_terms_version, source_user_type, staff_code, notes, created_at, updated_at
        )
        VALUES (
            '33333333-0001-4000-8000-000000000001',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            'ADMIN@ONEVERZE.LK',
            'PBKDF2-SHA256:100000:MyGLqisYhNcDqD5C4894Zg==:EzY4vyzeVpsHUIjG0eBEi9mpSdNv6p2+nFTBBBt5ujc=',
            NULL,
            NULL,
            'pbkdf2_embedded',
            'OneVerze Tenant Admin',
            'OneVerze Admin',
            '22222222-0001-4000-8000-000000000001',
            'OVZ-001',
            'admin',
            'ACTIVE',
            0,
            true,
            '1.0',
            'admin',
            'STAFF-001',
            'OneVerze seeded tenant admin user.',
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
            staff_code = EXCLUDED.staff_code,
            notes = EXCLUDED.notes,
            updated_at = now();

        INSERT INTO tenant_user_roles (
            id, tenant_id, user_id, role_id, assigned_by_tenant_user_id,
            assigned_at, revoked_at, created_at
        )
        SELECT
            '33333333-0003-4000-8000-000000000003',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
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
        WHERE users.tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
          AND users.email = 'ADMIN@ONEVERZE.LK'
        ON CONFLICT (tenant_id, user_id, role_id) DO UPDATE
        SET revoked_at = NULL,
            assigned_at = COALESCE(tenant_user_roles.assigned_at, EXCLUDED.assigned_at);

        INSERT INTO tenant_role_permissions (
            id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
            revoked_by_tenant_user_id, granted_at, revoked_at, notes, created_at
        )
        SELECT
            md5('oneverze:tenant_admin:' || permission_definitions.permission_code)::uuid,
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            roles.id,
            permission_definitions.id,
            users.id,
            NULL,
            now(),
            NULL,
            'OneVerze tenant admin permission seed.',
            now()
        FROM tenant_roles roles
        JOIN tenant_users users
          ON users.tenant_id = roles.tenant_id
         AND users.email = 'ADMIN@ONEVERZE.LK'
        CROSS JOIN permission_definitions
        WHERE roles.tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
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
                '44444444-0001-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                '22222222-0001-4000-8000-000000000001',
                'Front',
                1,
                'OneVerze Main Outlet Till 01',
                'OVZ-001-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000' AND email = 'ADMIN@ONEVERZE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000' AND email = 'ADMIN@ONEVERZE.LK' LIMIT 1),
                now(),
                now()
            ),
            (
                '44444444-0002-4000-8000-000000000002',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                '22222222-0002-4000-8000-000000000002',
                'Front',
                1,
                'OneVerze Outlet 02 Till 01',
                'OVZ-002-T01',
                'STANDARD',
                0,
                'LKR',
                true,
                'ACTIVE',
                (SELECT id FROM tenant_users WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000' AND email = 'ADMIN@ONEVERZE.LK' LIMIT 1),
                (SELECT id FROM tenant_users WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000' AND email = 'ADMIN@ONEVERZE.LK' LIMIT 1),
                now(),
                now()
            )
        ON CONFLICT (tenant_id, outlet_id, till_area_name, till_number) DO UPDATE
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
            '44444444-0001-4000-8000-000000000001',
            '44444444-0002-4000-8000-000000000002',
            '44444444-0003-4000-8000-000000000003'
        );

        DELETE FROM tenant_role_permissions
        WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
          AND role_id IN (
              SELECT id FROM tenant_roles
              WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
                AND role_code = 'TENANT_ADMIN'
          );

        DELETE FROM tenant_user_roles
        WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
          AND user_id IN (
              SELECT id FROM tenant_users
              WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
                AND email = 'ADMIN@ONEVERZE.LK'
          );

        DELETE FROM tenant_roles
        WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
          AND role_code = 'TENANT_ADMIN';

        DELETE FROM tenant_users
        WHERE tenant_id = '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'
          AND email = 'ADMIN@ONEVERZE.LK';
        """;
}
