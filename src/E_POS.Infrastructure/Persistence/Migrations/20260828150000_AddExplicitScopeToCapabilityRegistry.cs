using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260828150000_AddExplicitScopeToCapabilityRegistry")]
public partial class AddExplicitScopeToCapabilityRegistry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- 1. Add scope column to platform_modules
            ALTER TABLE platform_modules
            ADD COLUMN IF NOT EXISTS scope varchar(30) NOT NULL DEFAULT 'TENANT';

            -- 2. Add scope column to platform_features
            ALTER TABLE platform_features
            ADD COLUMN IF NOT EXISTS scope varchar(30) NOT NULL DEFAULT 'TENANT';

            -- 3. Add scope column to permission_definitions
            ALTER TABLE permission_definitions
            ADD COLUMN IF NOT EXISTS scope varchar(30) NOT NULL DEFAULT 'TENANT';

            -- 4. Explicitly mark platform scope modules
            UPDATE platform_modules
            SET scope = 'PLATFORM',
                updated_at = now()
            WHERE module_code IN (
                'authentication',
                'tenant_management',
                'user_management',
                'role_permission_management',
                'billing_core',
                'notification_system',
                'integration_core',
                'audit_logging',
                'master_data'
            );

            -- 5. Explicitly mark platform scope features
            UPDATE platform_features
            SET scope = 'PLATFORM',
                updated_at = now()
            FROM platform_modules pm
            WHERE platform_features.platform_module_id = pm.id
              AND pm.scope = 'PLATFORM';

            -- 6. Explicitly mark platform scope permission definitions
            UPDATE permission_definitions
            SET scope = 'PLATFORM',
                updated_at = now()
            WHERE permission_code LIKE 'platform.%';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE permission_definitions DROP COLUMN IF EXISTS scope;
            ALTER TABLE platform_features DROP COLUMN IF EXISTS scope;
            ALTER TABLE platform_modules DROP COLUMN IF EXISTS scope;
            """);
    }
}
