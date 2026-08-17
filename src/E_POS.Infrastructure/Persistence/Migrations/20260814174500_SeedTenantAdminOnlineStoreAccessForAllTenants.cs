using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260814174500_SeedTenantAdminOnlineStoreAccessForAllTenants")]
public partial class SeedTenantAdminOnlineStoreAccessForAllTenants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO tenant_feature_entitlements (
                id,
                tenant_id,
                platform_feature_id,
                feature_id,
                entitlement_status,
                source_type,
                is_enabled,
                effective_from,
                effective_until,
                created_at,
                updated_at
            )
            SELECT
                md5(tenants.id::text || ':' || platform_features.id::text)::uuid,
                tenants.id,
                platform_features.id,
                platform_features.id,
                'ENABLED',
                'MANUAL',
                true,
                now(),
                NULL,
                now(),
                now()
            FROM tenants
            CROSS JOIN platform_features
            WHERE platform_features.feature_code = 'online_store'
            ON CONFLICT (tenant_id, platform_feature_id) DO UPDATE
            SET entitlement_status = 'ENABLED',
                is_enabled = true,
                effective_until = NULL,
                updated_at = now();

            INSERT INTO tenant_role_permissions (
                id,
                tenant_id,
                role_id,
                permission_id,
                granted_by_tenant_user_id,
                granted_at,
                notes,
                created_at
            )
            SELECT
                md5(tenant_roles.id::text || ':' || permission_definitions.permission_code)::uuid,
                tenant_roles.tenant_id,
                tenant_roles.id,
                permission_definitions.id,
                NULL,
                now(),
                'Tenant admin online store access seed for all tenants.',
                now()
            FROM tenant_roles
            CROSS JOIN permission_definitions
            WHERE tenant_roles.role_code = 'TENANT_ADMIN'
              AND permission_definitions.permission_code IN (
                  'tenant.online_store.view',
                  'tenant.online_store.manage',
                  'tenant.online_store.publish',
                  'tenant.online_store.domains.manage',
                  'tenant.online_store.branding.manage',
                  'tenant.online_store.support.manage',
                  'tenant.online_store.fulfillment.manage',
                  'tenant.online_store.catalog.manage',
                  'tenant.online_store.policies.manage'
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM tenant_role_permissions existing_permission
                  WHERE existing_permission.tenant_id = tenant_roles.tenant_id
                    AND existing_permission.role_id = tenant_roles.id
                    AND existing_permission.permission_id = permission_definitions.id
              )
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
