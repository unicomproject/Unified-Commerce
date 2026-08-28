using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260823113000_RecoverEmptyCustomCashierRoleAssignments")]
public partial class RecoverEmptyCustomCashierRoleAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            DECLARE
                legacy_role RECORD;
            BEGIN
                FOR legacy_role IN
                    SELECT legacy.id AS legacy_role_id,
                           legacy.tenant_id,
                           system_cashier.id AS system_cashier_role_id
                    FROM tenant_roles legacy
                    JOIN tenant_roles system_cashier
                      ON system_cashier.tenant_id = legacy.tenant_id
                     AND system_cashier.role_code = 'CASHIER'
                     AND system_cashier.is_active
                     AND system_cashier.is_custom = FALSE
                    WHERE legacy.is_custom
                      AND legacy.is_active
                      AND legacy.role_code = 'CUSTOM_CASHIER'
                      AND lower(trim(legacy.role_name)) = 'custom cashier'
                      AND NOT EXISTS (
                          SELECT 1
                          FROM tenant_role_permissions legacy_permission
                          WHERE legacy_permission.tenant_id = legacy.tenant_id
                            AND legacy_permission.role_id = legacy.id
                            AND legacy_permission.revoked_at IS NULL)
                LOOP
                    INSERT INTO tenant_user_roles (
                        id, tenant_id, user_id, role_id,
                        assigned_by_tenant_user_id, assigned_at, revoked_at, created_at)
                    SELECT
                        md5(legacy_assignment.tenant_id::text || ':' || legacy_assignment.user_id::text || ':' ||
                            legacy_role.system_cashier_role_id::text)::uuid,
                        legacy_assignment.tenant_id,
                        legacy_assignment.user_id,
                        legacy_role.system_cashier_role_id,
                        legacy_assignment.assigned_by_tenant_user_id,
                        now(),
                        NULL,
                        now()
                    FROM tenant_user_roles legacy_assignment
                    WHERE legacy_assignment.tenant_id = legacy_role.tenant_id
                      AND legacy_assignment.role_id = legacy_role.legacy_role_id
                      AND legacy_assignment.revoked_at IS NULL
                    ON CONFLICT (tenant_id, user_id, role_id) DO UPDATE
                    SET revoked_at = NULL,
                        assigned_by_tenant_user_id = EXCLUDED.assigned_by_tenant_user_id,
                        assigned_at = EXCLUDED.assigned_at;

                    UPDATE tenant_user_roles
                    SET revoked_at = now()
                    WHERE tenant_id = legacy_role.tenant_id
                      AND role_id = legacy_role.legacy_role_id
                      AND revoked_at IS NULL;

                    INSERT INTO outlet_user_roles (
                        id, tenant_id, outlet_id, user_id, role_id,
                        assigned_by_tenant_user_id, assigned_at, revoked_by_tenant_user_id,
                        revoked_at, is_primary_manager, created_at)
                    SELECT
                        md5(legacy_assignment.tenant_id::text || ':' || legacy_assignment.outlet_id::text || ':' ||
                            legacy_assignment.user_id::text || ':' || legacy_role.system_cashier_role_id::text)::uuid,
                        legacy_assignment.tenant_id,
                        legacy_assignment.outlet_id,
                        legacy_assignment.user_id,
                        legacy_role.system_cashier_role_id,
                        legacy_assignment.assigned_by_tenant_user_id,
                        now(),
                        NULL,
                        NULL,
                        FALSE,
                        now()
                    FROM outlet_user_roles legacy_assignment
                    WHERE legacy_assignment.tenant_id = legacy_role.tenant_id
                      AND legacy_assignment.role_id = legacy_role.legacy_role_id
                      AND legacy_assignment.revoked_at IS NULL
                    ON CONFLICT (tenant_id, outlet_id, user_id, role_id) DO UPDATE
                    SET revoked_at = NULL,
                        revoked_by_tenant_user_id = NULL,
                        assigned_by_tenant_user_id = EXCLUDED.assigned_by_tenant_user_id,
                        assigned_at = EXCLUDED.assigned_at;

                    UPDATE outlet_user_roles
                    SET revoked_at = now(),
                        revoked_by_tenant_user_id = NULL
                    WHERE tenant_id = legacy_role.tenant_id
                      AND role_id = legacy_role.legacy_role_id
                      AND revoked_at IS NULL;

                    UPDATE tenant_roles
                    SET is_active = FALSE,
                        updated_at = now()
                    WHERE id = legacy_role.legacy_role_id;
                END LOOP;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
