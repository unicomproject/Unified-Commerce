using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260728190000_SeedPosReceiptReprintPermission")]
public partial class SeedPosReceiptReprintPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, created_at, updated_at)
            VALUES (
                'a4d1b0c2-8f31-4a2b-9f76-50a8b98d9101',
                'receipts.reprint',
                '71000000-0000-0000-0000-000000000010',
                '72000000-0000-0000-0000-000000000015',
                'reprint',
                'Reprint persisted POS receipts with an audited reason.',
                TRUE, TRUE, now(), now())
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                updated_at = now();

            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id,
                granted_by_tenant_user_id, granted_at, notes, created_at)
            SELECT
                gen_random_uuid(),
                '55555555-0000-4000-8000-000000000001',
                '88888888-0003-4000-8000-000000000001',
                id, NULL, now(),
                'Development cashier receipt reprint permission seed.',
                now()
            FROM permission_definitions
            WHERE permission_code = 'receipts.reprint'
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                notes = EXCLUDED.notes;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM tenant_role_permissions
            WHERE permission_id = 'a4d1b0c2-8f31-4a2b-9f76-50a8b98d9101';
            DELETE FROM permission_definitions
            WHERE id = 'a4d1b0c2-8f31-4a2b-9f76-50a8b98d9101';
            """);
    }
}
