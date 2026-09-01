using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs missing canonical bootstrap permission <c>tenant.outlets.details.view</c>.
/// Historical seed <c>20260708151816_SeedTenantAdminOutletDetailPermissions</c> used UUID
/// <c>77777777-0020-4000-8000-000000000001</c>, which already belonged to <c>tenant.tills.delete</c>,
/// so <c>ON CONFLICT (id) DO NOTHING</c> skipped the insert.
/// Also remaps the outlet_management bootstrap pack feature/module keys when unset.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260813220000_SeedTenantOutletsDetailsViewPermission")]
public partial class SeedTenantOutletsDetailsViewPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Insert missing canonical permission using a non-colliding stable id.
            -- Resolve module/feature by business keys (outlet_management / outlet_till_core).
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
                updated_at
            )
            SELECT
                '77777777-00a0-4000-8000-000000000001'::uuid,
                'tenant.outlets.details.view',
                COALESCE(
                    (SELECT platform_module_id FROM platform_features WHERE feature_code = 'outlet_management' LIMIT 1),
                    (SELECT id FROM platform_modules WHERE module_code = 'outlet_till_core' LIMIT 1),
                    (SELECT module_id FROM permission_definitions WHERE permission_code = 'tenant.outlets.view' LIMIT 1),
                    '00000000-0000-0000-0000-000000000000'::uuid
                ),
                COALESCE(
                    (SELECT id FROM platform_features WHERE feature_code = 'outlet_management' LIMIT 1),
                    (SELECT feature_id FROM permission_definitions WHERE permission_code = 'tenant.outlets.view' LIMIT 1),
                    '00000000-0000-0000-0000-000000000000'::uuid
                ),
                'view',
                'View outlet details',
                true,
                true,
                now(),
                now()
            WHERE NOT EXISTS (
                SELECT 1
                FROM permission_definitions existing
                WHERE existing.permission_code = 'tenant.outlets.details.view'
            )
            AND NOT EXISTS (
                SELECT 1
                FROM permission_definitions existing
                WHERE existing.id = '77777777-00a0-4000-8000-000000000001'::uuid
            );

            -- Ensure ACTIVE if a prior inactive row somehow exists under the canonical code.
            UPDATE permission_definitions
            SET
                is_active = true,
                action_type = COALESCE(NULLIF(action_type, ''), 'view'),
                description = COALESCE(NULLIF(description, ''), 'View outlet details'),
                updated_at = now()
            WHERE permission_code = 'tenant.outlets.details.view'
              AND is_active = false;

            -- Remap outlet_management bootstrap pack to canonical feature/module when unset/empty.
            UPDATE permission_definitions pd
            SET
                feature_id = pf.id,
                module_id = COALESCE(pf.platform_module_id, pd.module_id),
                updated_at = now()
            FROM platform_features pf
            WHERE pf.feature_code = 'outlet_management'
              AND pd.permission_code IN (
                  'tenant.outlets.view',
                  'tenant.outlets.details.view',
                  'tenant.outlets.update',
                  'tenant.outlets.manage'
              )
              AND (
                  pd.feature_id IS NULL
                  OR pd.feature_id = '00000000-0000-0000-0000-000000000000'::uuid
                  OR pd.feature_id <> pf.id
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM tenant_role_permissions
            USING permission_definitions
            WHERE tenant_role_permissions.permission_id = permission_definitions.id
              AND permission_definitions.permission_code = 'tenant.outlets.details.view'
              AND permission_definitions.id = '77777777-00a0-4000-8000-000000000001'::uuid;

            DELETE FROM permission_definitions
            WHERE permission_code = 'tenant.outlets.details.view'
              AND id = '77777777-00a0-4000-8000-000000000001'::uuid;
            """);
    }
}
