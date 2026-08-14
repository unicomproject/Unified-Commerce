using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Selected-Tenant runtime closure repairs:
/// 1) Allow custom tenant roles without a source template version (matches EF IsRequired(false)).
/// 2) Widen subscription history change_type so bootstrap audit action codes fit.
/// 3) Ensure global EA unit of measure exists for product bootstrap.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260813014500_SelectedTenantBootstrapRuntimeSchemaRepairs")]
public partial class SelectedTenantBootstrapRuntimeSchemaRepairs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE tenant_roles
                ALTER COLUMN source_role_template_version_id DROP NOT NULL;

            ALTER TABLE tenant_subscription_history
                ALTER COLUMN change_type TYPE character varying(80);

            INSERT INTO unit_of_measures (
                id, tenant_id, uom_name, conversion_factor, uom_code, created_at, updated_at, base_uom_id, status, symbol, uom_type
            )
            SELECT
                '91000000-0000-4000-8000-0000000000ea'::uuid,
                NULL,
                'Each',
                1.0,
                'EA',
                now(),
                now(),
                NULL,
                'ACTIVE',
                'ea',
                'COUNT'
            WHERE NOT EXISTS (
                SELECT 1 FROM unit_of_measures existing
                WHERE existing.tenant_id IS NULL AND existing.uom_code = 'EA'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM unit_of_measures
            WHERE id = '91000000-0000-4000-8000-0000000000ea'::uuid
              AND tenant_id IS NULL
              AND uom_code = 'EA';

            -- Do not re-apply NOT NULL on source_role_template_version_id (custom roles may exist).
            ALTER TABLE tenant_subscription_history
                ALTER COLUMN change_type TYPE character varying(40);
            """);
    }
}
