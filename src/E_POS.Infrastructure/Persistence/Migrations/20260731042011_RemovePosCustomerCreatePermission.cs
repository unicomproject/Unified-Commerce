using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePosCustomerCreatePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM tenant_role_permissions WHERE permission_id = '77777777-0312-4000-8000-000000000001';");
            migrationBuilder.Sql("DELETE FROM permission_definitions WHERE id = '77777777-0312-4000-8000-000000000001';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
                    updated_at)
                VALUES (
                    '77777777-0312-4000-8000-000000000001',
                    'customers.create',
                    '71000000-0000-0000-0000-000000000010',
                    '72000000-0000-0000-0000-000000000013',
                    'create',
                    'Create customers on POS.',
                    TRUE,
                    TRUE,
                    now(),
                    now())
                ON CONFLICT (permission_code) DO UPDATE
                SET module_id = EXCLUDED.module_id,
                    feature_id = EXCLUDED.feature_id,
                    action_type = EXCLUDED.action_type,
                    description = EXCLUDED.description,
                    is_system = TRUE,
                    is_active = TRUE,
                    updated_at = now();
            ");

            migrationBuilder.Sql(@"
                INSERT INTO tenant_role_permissions (
                    id,
                    tenant_id,
                    role_id,
                    permission_id,
                    granted_by_tenant_user_id,
                    granted_at,
                    notes,
                    created_at)
                VALUES (
                    gen_random_uuid(),
                    '55555555-0000-4000-8000-000000000001',
                    '88888888-0003-4000-8000-000000000001',
                    '77777777-0312-4000-8000-000000000001',
                    NULL,
                    now(),
                    'Development cashier POS permission seed.',
                    now())
                ON CONFLICT DO NOTHING;
            ");
        }
    }
}
