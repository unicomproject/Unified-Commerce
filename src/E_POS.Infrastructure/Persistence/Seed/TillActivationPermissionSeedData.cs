namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>Catalog entry and conservative default grant for tenant administrators.</summary>
public static class TillActivationPermissionSeedData
{
    public const string UpSql = """
        INSERT INTO permission_definitions
            (id, permission_code, module_id, feature_id, action_type, description,
             is_system, is_active, scope, created_at, updated_at)
        SELECT '4a8ac541-f21d-475b-947e-a2388aaea201'::uuid,
            'till.activation_code.generate', feature.platform_module_id, feature.id,
            'generate', 'Generate a single-use POS device activation code for an authorized till.',
            TRUE, TRUE, 'TENANT', now(), now()
        FROM platform_features feature
        JOIN platform_modules module ON module.id = feature.platform_module_id
        WHERE feature.feature_code = 'till_management' AND module.module_code = 'outlet_till_core'
        ON CONFLICT (permission_code) DO UPDATE
        SET module_id = EXCLUDED.module_id, feature_id = EXCLUDED.feature_id,
            action_type = EXCLUDED.action_type, description = EXCLUDED.description,
            scope = EXCLUDED.scope, updated_at = now();

        INSERT INTO tenant_role_permissions
            (id, tenant_id, role_id, permission_id, granted_by_tenant_user_id, granted_at, notes, created_at)
        SELECT md5(role.id::text || ':' || permission.id::text)::uuid,
            role.tenant_id, role.id, permission.id, NULL, now(),
            'Till activation code permission for tenant administrators.', now()
        FROM tenant_roles role
        JOIN permission_definitions permission ON permission.permission_code = 'till.activation_code.generate'
        WHERE role.role_code = 'TENANT_ADMIN' AND role.is_active AND permission.is_active
        ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
        """;
}
