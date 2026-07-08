namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentCashierRoleSeedData
{
    public static string UpSql => $"""
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
        VALUES (
            '{DevelopmentTenantSeedConstants.CashierRoleId}',
            '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
            '66666666-0000-4000-8000-000000000001',
            '66666666-0001-4000-8000-000000000001',
            '{DevelopmentTenantSeedConstants.CashierRoleCode}',
            'Cashier',
            'Development POS cashier.',
            FALSE,
            TRUE,
            '{DevelopmentTenantSeedConstants.CashierUserId}',
            '{DevelopmentTenantSeedConstants.CashierUserId}',
            now(),
            now())
        ON CONFLICT (tenant_id, role_code) DO UPDATE
        SET role_name = EXCLUDED.role_name,
            source_role_template_id = EXCLUDED.source_role_template_id,
            source_role_template_version_id = EXCLUDED.source_role_template_version_id,
            role_description = EXCLUDED.role_description,
            is_active = TRUE,
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();

        INSERT INTO tenant_user_roles (
            id,
            tenant_id,
            user_id,
            role_id,
            assigned_by_tenant_user_id,
            assigned_at,
            created_at)
        VALUES (
            'aaaaaaaa-0003-4000-8000-000000000001',
            '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
            '{DevelopmentTenantSeedConstants.CashierUserId}',
            '{DevelopmentTenantSeedConstants.CashierRoleId}',
            NULL,
            now(),
            now())
        ON CONFLICT (tenant_id, user_id, role_id) DO UPDATE
        SET revoked_at = NULL,
            assigned_at = COALESCE(tenant_user_roles.assigned_at, EXCLUDED.assigned_at);
        """;

    public static string DownSql => $"""
        DELETE FROM tenant_user_roles
        WHERE tenant_id = '{DevelopmentTenantSeedConstants.DevelopmentTenantId}'
          AND user_id = '{DevelopmentTenantSeedConstants.CashierUserId}'
          AND role_id = '{DevelopmentTenantSeedConstants.CashierRoleId}';
        """;
}
