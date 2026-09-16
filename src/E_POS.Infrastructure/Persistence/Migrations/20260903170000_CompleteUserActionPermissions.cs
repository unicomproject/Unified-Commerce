using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903170000_CompleteUserActionPermissions")]
public partial class CompleteUserActionPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH requested(permission_code, action_type, description) AS (
                VALUES
                    ('tenant.users.roles.assign', 'assign', 'Assign or replace a tenant user role.'),
                    ('tenant.users.outlets.assign', 'assign', 'Configure outlet access for a tenant user.'),
                    ('tenant.users.tills.assign', 'assign', 'Configure till access for a tenant user.'),
                    ('tenant.users.invites.resend', 'resend', 'Resend a pending tenant user invitation.'),
                    ('tenant.users.invites.revoke', 'revoke', 'Revoke a pending tenant user invitation.')
            )
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5('permission:' || requested.permission_code)::uuid,
                requested.permission_code,
                template.module_id,
                template.feature_id,
                requested.action_type,
                requested.description,
                TRUE, TRUE, 'TENANT', now(), now()
            FROM requested
            JOIN permission_definitions template
              ON template.permission_code = 'tenant.users.manage'
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                scope = 'TENANT',
                updated_at = now();

            WITH grants(permission_code, predecessor_codes) AS (
                VALUES
                    ('tenant.users.disable', ARRAY['tenant.users.update']),
                    ('tenant.users.roles.assign', ARRAY['tenant.users.create', 'tenant.users.invite', 'tenant.users.update']),
                    ('tenant.users.outlets.assign', ARRAY['tenant.users.create', 'tenant.users.invite', 'tenant.users.update']),
                    ('tenant.users.tills.assign', ARRAY['tenant.users.create', 'tenant.users.invite', 'tenant.users.update']),
                    ('tenant.users.invites.resend', ARRAY['tenant.users.invite']),
                    ('tenant.users.invites.revoke', ARRAY['tenant.users.invite'])
            )
            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
                granted_at, notes, created_at)
            SELECT
                md5(role.id::text || ':' || permission.id::text)::uuid,
                role.tenant_id,
                role.id,
                permission.id,
                NULL,
                now(),
                'Granular Tenant User action permission completion.',
                now()
            FROM tenant_roles role
            CROSS JOIN grants
            JOIN permission_definitions permission
              ON permission.permission_code = grants.permission_code
             AND permission.is_active
            WHERE role.is_active
              AND (
                    role.role_code = 'TENANT_ADMIN'
                    OR EXISTS (
                        SELECT 1
                        FROM tenant_role_permissions existing_grant
                        JOIN permission_definitions existing_permission
                          ON existing_permission.id = existing_grant.permission_id
                        WHERE existing_grant.tenant_id = role.tenant_id
                          AND existing_grant.role_id = role.id
                          AND existing_grant.revoked_at IS NULL
                          AND (
                              existing_permission.permission_code = 'tenant.users.manage'
                              OR existing_permission.permission_code = ANY(grants.predecessor_codes)))
              )
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                granted_at = now(),
                notes = EXCLUDED.notes;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Grants are retained because tenant administrators may edit them later.
    }
}
