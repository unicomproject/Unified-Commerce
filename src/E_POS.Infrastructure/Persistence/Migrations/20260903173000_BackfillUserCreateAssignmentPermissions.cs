using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903173000_BackfillUserCreateAssignmentPermissions")]
public partial class BackfillUserCreateAssignmentPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH assignment_permissions(permission_code) AS (
                VALUES
                    ('tenant.users.roles.assign'),
                    ('tenant.users.outlets.assign'),
                    ('tenant.users.tills.assign')
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
                'Backfill User creation assignment permissions.',
                now()
            FROM tenant_roles role
            CROSS JOIN assignment_permissions requested
            JOIN permission_definitions permission
              ON permission.permission_code = requested.permission_code
             AND permission.is_active
            WHERE role.is_active
              AND EXISTS (
                    SELECT 1
                    FROM tenant_role_permissions existing_grant
                    JOIN permission_definitions existing_permission
                      ON existing_permission.id = existing_grant.permission_id
                    WHERE existing_grant.tenant_id = role.tenant_id
                      AND existing_grant.role_id = role.id
                      AND existing_grant.revoked_at IS NULL
                      AND existing_permission.permission_code IN (
                          'tenant.users.create',
                          'tenant.users.invite'))
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
