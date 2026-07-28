namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentTenantAdminAccessSeedData
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
        VALUES (
            '71500000-0000-0000-0000-000000000010',
            'tenant_admin',
            'Tenant Admin',
            'Tenant administration dashboard and operations.',
            'ACTIVE',
            4,
            'tenant_admin',
            'Tenant Admin',
            true,
            now(),
            now())
        ON CONFLICT (module_code) DO UPDATE
        SET name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            module_key = EXCLUDED.module_key,
            module_name = EXCLUDED.module_name,
            is_core_module = true,
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
        SELECT features.feature_id, pm.id, features.feature_code, features.feature_name, features.description, 'ACTIVE', features.sort_order, features.feature_key, features.feature_name, true, now(), now()
        FROM platform_modules pm
        CROSS JOIN (
            VALUES
                ('72500000-0000-0000-0000-000000000010'::uuid, 'tenant_admin.dashboard', 'tenant_admin.dashboard', 'Tenant Admin Dashboard', 'Tenant admin dashboard access.', 10),
                ('72500000-0000-0000-0000-000000000011'::uuid, 'tenant_admin.outlets', 'tenant_admin.outlets', 'Tenant Admin Outlets', 'Tenant outlet management access.', 20),
                ('72500000-0000-0000-0000-000000000012'::uuid, 'tenant.tills', 'tenant.tills', 'Tenant Tills', 'Tenant till management access.', 30),
                ('72500000-0000-0000-0000-000000000013'::uuid, 'tenant.users', 'tenant.users', 'Tenant Users', 'Tenant user management access.', 40),
                ('72500000-0000-0000-0000-000000000014'::uuid, 'tenant.roles', 'tenant.roles', 'Tenant Roles', 'Tenant role and permission access.', 50),
                ('72500000-0000-0000-0000-000000000015'::uuid, 'catalog.product', 'catalog.product', 'Tenant Products', 'Tenant product catalogue access.', 60),
                ('72500000-0000-0000-0000-000000000016'::uuid, 'inventory.stock', 'inventory.stock', 'Tenant Stock', 'Tenant stock management access.', 70),
                ('72500000-0000-0000-0000-000000000017'::uuid, 'reports', 'reports', 'Tenant Reports', 'Tenant reporting access.', 80),
                ('72500000-0000-0000-0000-000000000018'::uuid, 'subscription.billing', 'subscription.billing', 'Tenant Billing', 'Tenant billing access.', 90),
                ('72500000-0000-0000-0000-000000000019'::uuid, 'tenant.settings', 'tenant.settings', 'Tenant Settings', 'Tenant settings access.', 100),
                ('72500000-0000-0000-0000-000000000020'::uuid, 'tenant.activity', 'tenant.activity', 'Tenant Activity', 'Tenant activity log access.', 110)
        ) AS features(feature_id, feature_code, feature_key, feature_name, description, sort_order)
        WHERE pm.module_code = 'tenant_admin'
        ON CONFLICT (feature_key) DO UPDATE
        SET platform_module_id = EXCLUDED.platform_module_id,
            feature_code = EXCLUDED.feature_code,
            name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            feature_name = EXCLUDED.feature_name,
            is_core_feature = true,
            updated_at = now();

        INSERT INTO permission_definitions (
            id,
            permission_code,
            module_id,
            feature_id,
            action_type,
            description,
            is_system,
            is_active,
            created_at,
            updated_at)
        SELECT permissions.permission_id, permissions.permission_code, pm.id, pf.id, permissions.action_type, permissions.description, true, true, now(), now()
        FROM (
            VALUES
                ('77777777-1001-4000-8000-000000000001'::uuid, 'tenant.dashboard.view', 'tenant_admin.dashboard', 'view', 'View Tenant Admin dashboard'),
                ('77777777-1002-4000-8000-000000000001'::uuid, 'tenant.settings.manage', 'tenant.settings', 'manage', 'Manage tenant settings'),
                ('77777777-1003-4000-8000-000000000001'::uuid, 'tenant.users.manage', 'tenant.users', 'manage', 'Manage tenant users'),
                ('77777777-1004-4000-8000-000000000001'::uuid, 'tenant.roles.manage', 'tenant.roles', 'manage', 'Manage tenant roles and permissions'),
                ('77777777-1005-4000-8000-000000000001'::uuid, 'tenant.outlets.manage', 'tenant_admin.outlets', 'manage', 'Manage outlets'),
                ('77777777-1006-4000-8000-000000000001'::uuid, 'tenant.till.manage', 'tenant.tills', 'manage', 'Manage tills'),
                ('77777777-1007-4000-8000-000000000001'::uuid, 'tenant.products.view', 'catalog.product', 'view', 'View tenant products'),
                ('77777777-1008-4000-8000-000000000001'::uuid, 'tenant.products.create', 'catalog.product', 'create', 'Create tenant products'),
                ('77777777-1009-4000-8000-000000000001'::uuid, 'tenant.products.update', 'catalog.product', 'update', 'Update tenant products'),
                ('77777777-1010-4000-8000-000000000001'::uuid, 'tenant.stock.view', 'inventory.stock', 'view', 'View tenant stock'),
                ('77777777-1011-4000-8000-000000000001'::uuid, 'tenant.reports.sales.view', 'reports', 'view', 'View tenant sales reports'),
                ('77777777-1012-4000-8000-000000000001'::uuid, 'tenant.billing.view', 'subscription.billing', 'view', 'View tenant billing'),
                ('77777777-1013-4000-8000-000000000001'::uuid, 'tenant.activity.view', 'tenant.activity', 'view', 'View tenant activity'),
                ('77777777-1014-4000-8000-000000000001'::uuid, 'catalog.products.view', 'catalog.product', 'view', 'View product catalogue'),
                ('77777777-1015-4000-8000-000000000001'::uuid, 'catalog.products.create', 'catalog.product', 'create', 'Create product catalogue entries'),
                ('77777777-1016-4000-8000-000000000001'::uuid, 'catalog.products.update', 'catalog.product', 'update', 'Update product catalogue entries'),
                ('77777777-1017-4000-8000-000000000001'::uuid, 'inventory.stock.view', 'inventory.stock', 'view', 'View stock inventory'),
                ('77777777-1018-4000-8000-000000000001'::uuid, 'reports.sales.view', 'reports', 'view', 'View sales reports')
        ) AS permissions(permission_id, permission_code, feature_code, action_type, description)
        JOIN platform_features pf ON pf.feature_code = permissions.feature_code
        JOIN platform_modules pm ON pm.id = pf.platform_module_id
        ON CONFLICT (permission_code) DO UPDATE
        SET module_id = EXCLUDED.module_id,
            feature_id = EXCLUDED.feature_id,
            action_type = EXCLUDED.action_type,
            description = EXCLUDED.description,
            is_system = true,
            is_active = true,
            updated_at = now();

        UPDATE tenant_roles
        SET role_name = 'Tenant Admin',
            source_role_template_id = '66666666-0000-4000-8000-000000000001',
            source_role_template_version_id = '66666666-0001-4000-8000-000000000001',
            role_description = 'Development tenant administrator.',
            is_active = true,
            updated_by_tenant_user_id = '99999999-0001-4000-8000-000000000001',
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND role_code = 'TENANT_ADMIN';

        INSERT INTO tenant_roles (
            id,
            tenant_id,
            source_role_template_id,
            source_role_template_version_id,
            role_code,
            role_name,
            role_description,
            is_custom,
            is_active,
            created_by_tenant_user_id,
            updated_by_tenant_user_id,
            created_at,
            updated_at)
        SELECT
            '88888888-0001-4000-8000-000000000001',
            tenants.id,
            '66666666-0000-4000-8000-000000000001',
            '66666666-0001-4000-8000-000000000001',
            'TENANT_ADMIN',
            'Tenant Admin',
            'Development tenant administrator.',
            false,
            true,
            tenant_users.id,
            tenant_users.id,
            now(),
            now()
        FROM tenants
        LEFT JOIN tenant_users
            ON tenant_users.id = '99999999-0001-4000-8000-000000000001'
        WHERE tenants.id = '55555555-0000-4000-8000-000000000001'
          AND NOT EXISTS (
              SELECT 1
              FROM tenant_roles existing_roles
              WHERE existing_roles.tenant_id = tenants.id
                AND existing_roles.role_code = 'TENANT_ADMIN');

        UPDATE tenant_user_roles
        SET revoked_at = NULL,
            assigned_at = COALESCE(assigned_at, now())
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND user_id = '99999999-0001-4000-8000-000000000001'
          AND role_id = (
              SELECT id
              FROM tenant_roles
              WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
                AND role_code = 'TENANT_ADMIN'
              LIMIT 1);

        INSERT INTO tenant_user_roles (
            id,
            tenant_id,
            user_id,
            role_id,
            assigned_by_tenant_user_id,
            assigned_at,
            revoked_at,
            created_at)
        SELECT
            'aaaaaaaa-0001-4000-8000-000000000001',
            tenant_users.tenant_id,
            tenant_users.id,
            tenant_roles.id,
            NULL,
            now(),
            NULL,
            now()
        FROM tenant_users
        JOIN tenant_roles
            ON tenant_roles.tenant_id = tenant_users.tenant_id
           AND tenant_roles.role_code = 'TENANT_ADMIN'
        WHERE tenant_users.id = '99999999-0001-4000-8000-000000000001'
          AND tenant_users.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND NOT EXISTS (
              SELECT 1
              FROM tenant_user_roles existing_user_roles
              WHERE existing_user_roles.tenant_id = tenant_users.tenant_id
                AND existing_user_roles.user_id = tenant_users.id
                AND existing_user_roles.role_id = tenant_roles.id);

        UPDATE tenant_role_permissions
        SET revoked_at = NULL,
            revoked_by_tenant_user_id = NULL,
            notes = 'Development tenant admin access repair seed.'
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND EXISTS (
              SELECT 1
              FROM tenant_roles
              WHERE tenant_roles.tenant_id = tenant_role_permissions.tenant_id
                AND tenant_roles.id = tenant_role_permissions.role_id
                AND tenant_roles.role_code = 'TENANT_ADMIN')
          AND EXISTS (
              SELECT 1
              FROM permission_definitions
              WHERE permission_definitions.id = tenant_role_permissions.permission_id
                AND permission_definitions.permission_code IN (
                    'tenant.dashboard.view',
                    'tenant.settings.manage',
                    'tenant.users.manage',
                    'tenant.roles.manage',
                    'tenant.outlets.manage',
                    'tenant.till.manage',
                    'tenant.products.view',
                    'tenant.products.create',
                    'tenant.products.update',
                    'tenant.stock.view',
                    'tenant.reports.sales.view',
                    'tenant.billing.view',
                    'tenant.activity.view',
                    'catalog.products.view',
                    'catalog.products.create',
                    'catalog.products.update',
                    'inventory.stock.view',
                    'reports.sales.view'));

        INSERT INTO tenant_role_permissions (
            id,
            tenant_id,
            role_id,
            permission_id,
            granted_by_tenant_user_id,
            granted_at,
            revoked_at,
            notes,
            created_at)
        SELECT
            md5('TENANT_ADMIN:' || permission_definitions.permission_code)::uuid,
            tenant_roles.tenant_id,
            tenant_roles.id,
            permission_definitions.id,
            '99999999-0001-4000-8000-000000000001',
            now(),
            NULL,
            'Development tenant admin access repair seed.',
            now()
        FROM tenant_roles
        JOIN permission_definitions
            ON permission_definitions.permission_code IN (
                'tenant.dashboard.view',
                'tenant.settings.manage',
                'tenant.users.manage',
                'tenant.roles.manage',
                'tenant.outlets.manage',
                'tenant.till.manage',
                'tenant.products.view',
                'tenant.products.create',
                'tenant.products.update',
                'tenant.stock.view',
                'tenant.reports.sales.view',
                'tenant.billing.view',
                'tenant.activity.view',
                'catalog.products.view',
                'catalog.products.create',
                'catalog.products.update',
                'inventory.stock.view',
                'reports.sales.view')
        WHERE tenant_roles.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND tenant_roles.role_code = 'TENANT_ADMIN'
          AND NOT EXISTS (
              SELECT 1
              FROM tenant_role_permissions existing_role_permissions
              WHERE existing_role_permissions.tenant_id = tenant_roles.tenant_id
                AND existing_role_permissions.role_id = tenant_roles.id
                AND existing_role_permissions.permission_id = permission_definitions.id);

        UPDATE tenant_feature_entitlements
        SET entitlement_status = 'ENABLED',
            is_enabled = true,
            effective_from = COALESCE(effective_from, now()),
            effective_until = NULL,
            revoked_at = NULL,
            revoked_by_platform_user_id = NULL,
            revoked_reason = NULL,
            updated_at = now()
        FROM platform_features
        WHERE tenant_feature_entitlements.platform_feature_id = platform_features.id
          AND tenant_feature_entitlements.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND platform_features.feature_code IN (
              'tenant_admin.dashboard',
              'tenant_admin.outlets',
              'tenant.tills',
              'tenant.users',
              'tenant.roles',
              'catalog.product',
              'inventory.stock',
              'reports',
              'subscription.billing',
              'tenant.settings',
              'tenant.activity');

        INSERT INTO tenant_feature_entitlements (
            id,
            tenant_id,
            entitlement_status,
            platform_feature_id,
            feature_id,
            source_type,
            source_reference_id,
            is_enabled,
            effective_from,
            effective_until,
            revoked_at,
            created_at,
            updated_at)
        SELECT
            md5('DEV-TENANT-001:' || platform_features.feature_code)::uuid,
            '55555555-0000-4000-8000-000000000001',
            'ENABLED',
            platform_features.id,
            platform_features.id,
            'MANUAL',
            NULL,
            true,
            now(),
            NULL,
            NULL,
            now(),
            now()
        FROM platform_features
        WHERE platform_features.feature_code IN (
            'tenant_admin.dashboard',
            'tenant_admin.outlets',
            'tenant.tills',
            'tenant.users',
            'tenant.roles',
            'catalog.product',
            'inventory.stock',
            'reports',
            'subscription.billing',
            'tenant.settings',
            'tenant.activity')
          AND NOT EXISTS (
              SELECT 1
              FROM tenant_feature_entitlements existing_entitlements
              WHERE existing_entitlements.tenant_id = '55555555-0000-4000-8000-000000000001'
                AND existing_entitlements.platform_feature_id = platform_features.id);
        """;

    public const string DownSql = """
        DELETE FROM tenant_feature_entitlements
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND platform_feature_id IN (
              SELECT id FROM platform_features
              WHERE feature_code IN (
                  'tenant_admin.dashboard',
                  'tenant_admin.outlets',
                  'tenant.tills',
                  'tenant.users',
                  'tenant.roles',
                  'catalog.product',
                  'inventory.stock',
                  'reports',
                  'subscription.billing',
                  'tenant.settings',
                  'tenant.activity'));

        DELETE FROM tenant_role_permissions
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND role_id IN (
              SELECT id FROM tenant_roles
              WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
                AND role_code = 'TENANT_ADMIN');

        DELETE FROM tenant_user_roles
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND user_id = '99999999-0001-4000-8000-000000000001'
          AND role_id IN (
              SELECT id FROM tenant_roles
              WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
                AND role_code = 'TENANT_ADMIN');
        """;
}
