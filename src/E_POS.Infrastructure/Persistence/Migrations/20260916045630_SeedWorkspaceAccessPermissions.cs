using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedWorkspaceAccessPermissions : Migration
    {
        // Defines workspace.tenant_admin.access / workspace.pos.access — the two gate
        // permissions the client apps use to decide which top-level workspace (Tenant
        // Admin vs. POS) a signed-in user can land in. These codes never previously
        // existed in the catalog, so no user anywhere could pass that gate; every
        // tenant's Tenant Admin role is granted the admin gate, and every active role
        // (including Tenant Admin, since an owner may also run the till) is granted the
        // POS gate. New tenants get both automatically going forward via
        // TenantAdminBootstrapPermissionCatalog.BasePermissionCodes and
        // TenantRoleSetupCatalog.CashierAllowedPermissionCodes.
        public const string DefinePermissionsSql = """
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('workspace.tenant_admin.access')::uuid,
                'workspace.tenant_admin.access',
                reference.module_id, reference.feature_id, 'access',
                'Land in the Tenant Admin workspace after sign-in.', true, true, 'TENANT', now(), now()
            FROM permission_definitions reference
            WHERE reference.permission_code = 'tenant.dashboard.view'
            ON CONFLICT (permission_code) DO NOTHING;

            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('workspace.pos.access')::uuid,
                'workspace.pos.access',
                reference.module_id, reference.feature_id, 'access',
                'Land in the POS workspace after sign-in.', true, true, 'TENANT', now(), now()
            FROM permission_definitions reference
            WHERE reference.permission_code = 'pos.home.view'
            ON CONFLICT (permission_code) DO NOTHING;
            """;

        public const string BackfillGrantsSql = """
            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id,
                granted_by_tenant_user_id, granted_at, notes, created_at)
            SELECT
                md5('workspace-admin-access-role:' || tr.tenant_id::text || ':' || tr.id::text)::uuid,
                tr.tenant_id, tr.id, pd.id,
                NULL, now(), 'Workspace gate: Tenant Admin workspace access.', now()
            FROM tenant_roles tr
            JOIN permission_definitions pd ON pd.permission_code = 'workspace.tenant_admin.access'
            WHERE tr.role_code = 'TENANT_ADMIN' AND tr.is_active = TRUE
            ON CONFLICT DO NOTHING;

            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id,
                granted_by_tenant_user_id, granted_at, notes, created_at)
            SELECT
                md5('workspace-pos-access-role:' || tr.tenant_id::text || ':' || tr.id::text)::uuid,
                tr.tenant_id, tr.id, pd.id,
                NULL, now(), 'Workspace gate: POS workspace access.', now()
            FROM tenant_roles tr
            JOIN permission_definitions pd ON pd.permission_code = 'workspace.pos.access'
            WHERE tr.is_active = TRUE
            ON CONFLICT DO NOTHING;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DefinePermissionsSql);
            migrationBuilder.Sql(BackfillGrantsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_role_permissions
                WHERE notes IN (
                    'Workspace gate: Tenant Admin workspace access.',
                    'Workspace gate: POS workspace access.');

                DELETE FROM permission_definitions
                WHERE permission_code IN ('workspace.tenant_admin.access', 'workspace.pos.access');
                """);
        }
    }
}
