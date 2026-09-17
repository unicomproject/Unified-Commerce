namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>All-tenant additive provisioning. Existing grants, including revocations, are preserved.</summary>
public static class WorkspacePermissionSeedData
{
    public const string UpSql = """
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM permission_definitions WHERE permission_code = 'pos.till.open')
               OR NOT EXISTS (SELECT 1 FROM permission_definitions WHERE permission_code = 'tenant.dashboard.view')
            THEN RAISE EXCEPTION 'Workspace provisioning requires existing POS and tenant dashboard catalog anchors';
            END IF;
        END $$;

        INSERT INTO permission_definitions
            (id, permission_code, module_id, feature_id, action_type, description,
             is_system, is_active, scope, created_at, updated_at)
        SELECT gen_random_uuid(), mapping.code, anchor.module_id, anchor.feature_id,
               'access', mapping.description, TRUE, TRUE, 'TENANT', now(), now()
        FROM (VALUES
            ('workspace.pos.access', 'pos.till.open', 'Access the POS workspace; operational permissions remain separate.'),
            ('workspace.tenant_admin.access', 'tenant.dashboard.view', 'Access the Tenant Admin workspace; operational permissions remain separate.')
        ) AS mapping(code, anchor_code, description)
        JOIN permission_definitions anchor ON anchor.permission_code = mapping.anchor_code
        ON CONFLICT (permission_code) DO NOTHING;

        INSERT INTO tenant_role_permissions
            (id, tenant_id, role_id, permission_id, granted_at, notes, created_at)
        SELECT gen_random_uuid(), role.tenant_id, role.id, permission.id, now(),
               'Workspace access provisioning', now()
        FROM tenant_roles role
        JOIN (VALUES
            ('CASHIER', 'workspace.pos.access'),
            ('TENANT_ADMIN', 'workspace.tenant_admin.access')
        ) AS mapping(role_code, permission_code) ON role.role_code = mapping.role_code
        JOIN permission_definitions permission ON permission.permission_code = mapping.permission_code
        WHERE role.is_active AND permission.is_active
        ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
        """;
}
