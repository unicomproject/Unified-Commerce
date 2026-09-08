using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260904120000_GrantAllPosPermissionsToDevelopmentCashier001")]
public partial class GrantAllPosPermissionsToDevelopmentCashier001 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO tenant_user_permissions (
                id,
                tenant_id,
                user_id,
                permission_id,
                assigned_by_tenant_user_id,
                assigned_at,
                revoked_at,
                created_at)
            SELECT
                md5('development-cashier001-all-pos:' || permission_definitions.permission_code)::uuid,
                tenant_users.tenant_id,
                tenant_users.id,
                permission_definitions.id,
                NULL,
                now(),
                NULL,
                now()
            FROM tenant_users
            CROSS JOIN permission_definitions
            WHERE upper(tenant_users.email) = 'CASHIER001@GMAIL.COM'
              AND tenant_users.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
              AND tenant_users.account_status = 'ACTIVE'
              AND permission_definitions.is_active = TRUE
              AND permission_definitions.permission_code LIKE 'pos.%'
            ON CONFLICT (tenant_id, user_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                assigned_at = COALESCE(tenant_user_permissions.assigned_at, EXCLUDED.assigned_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM tenant_user_permissions
            USING tenant_users, permission_definitions
            WHERE tenant_user_permissions.user_id = tenant_users.id
              AND tenant_user_permissions.permission_id = permission_definitions.id
              AND tenant_user_permissions.id =
                  md5('development-cashier001-all-pos:' || permission_definitions.permission_code)::uuid
              AND upper(tenant_users.email) = 'CASHIER001@GMAIL.COM'
              AND tenant_users.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
              AND permission_definitions.permission_code LIKE 'pos.%';
            """);
    }
}
