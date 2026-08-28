using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260828160000_LinkPlatformPermissionsToCapabilityRegistry")]
public partial class LinkPlatformPermissionsToCapabilityRegistry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- 1. Add platform_module_id and platform_feature_id columns (nullable initially for backfill)
            ALTER TABLE platform_permissions
            ADD COLUMN IF NOT EXISTS platform_module_id uuid NULL,
            ADD COLUMN IF NOT EXISTS platform_feature_id uuid NULL;

            -- 2. Ensure default platform module and feature exist as safety fallback
            INSERT INTO platform_modules (id, module_code, module_key, module_name, name, description, status, sort_order, is_core_module, scope, created_at, updated_at)
            SELECT '11000000-0000-0000-0000-000000000001'::uuid, 'master_data', 'master_data', 'Master Data', 'Master Data', 'Platform master data module', 'ACTIVE', 1, true, 'PLATFORM', now(), now()
            WHERE NOT EXISTS (SELECT 1 FROM platform_modules WHERE module_code = 'master_data' OR module_key = 'master_data');

            INSERT INTO platform_features (id, platform_module_id, feature_code, feature_key, feature_name, name, description, status, sort_order, is_core_feature, scope, created_at, updated_at)
            SELECT '21000000-0000-0000-0000-000000000001'::uuid, pm.id, 'capability_catalog', 'capability_catalog', 'Capability Catalog', 'Capability Catalog', 'Capability catalog feature', 'ACTIVE', 1, true, 'PLATFORM', now(), now()
            FROM platform_modules pm WHERE (pm.module_code = 'master_data' OR pm.module_key = 'master_data')
            AND NOT EXISTS (SELECT 1 FROM platform_features WHERE feature_code = 'capability_catalog' OR feature_key = 'capability_catalog');

            -- 3. Deterministically backfill every platform_permission row with valid non-null foreign keys
            UPDATE platform_permissions pp
            SET platform_module_id = COALESCE(
                    (SELECT pm.id FROM platform_modules pm WHERE (pm.module_code = 'tenant_management' OR pm.module_key = 'tenant_management') AND (pp.permission_code LIKE 'platform.tenant%' OR pp.permission_code LIKE 'platform.tenants.%') LIMIT 1),
                    (SELECT pm.id FROM platform_modules pm WHERE (pm.module_code = 'billing_core' OR pm.module_key = 'billing_core') AND (pp.permission_code LIKE 'platform.subscription%' OR pp.permission_code LIKE 'platform.billing%') LIMIT 1),
                    (SELECT pm.id FROM platform_modules pm WHERE (pm.module_code = 'user_management' OR pm.module_key = 'user_management') AND pp.permission_code LIKE 'platform.user%' LIMIT 1),
                    (SELECT pm.id FROM platform_modules pm WHERE (pm.module_code = 'role_permission_management' OR pm.module_key = 'role_permission_management') AND (pp.permission_code LIKE 'platform.role%' OR pp.permission_code LIKE 'platform.permission%') LIMIT 1),
                    (SELECT pm.id FROM platform_modules pm WHERE (pm.module_code = 'audit_logging' OR pm.module_key = 'audit_logging') AND pp.permission_code LIKE 'platform.audit%' LIMIT 1),
                    (SELECT pm.id FROM platform_modules pm WHERE (pm.module_code = 'integration_core' OR pm.module_key = 'integration_core') AND pp.permission_code LIKE 'platform.integration%' LIMIT 1),
                    (SELECT id FROM platform_modules WHERE scope = 'PLATFORM' LIMIT 1),
                    (SELECT id FROM platform_modules LIMIT 1),
                    '11000000-0000-0000-0000-000000000001'::uuid),
                platform_feature_id = COALESCE(
                    (SELECT pf.id FROM platform_features pf JOIN platform_modules pm ON pf.platform_module_id = pm.id WHERE (pm.module_code = 'tenant_management' OR pm.module_key = 'tenant_management') AND (pp.permission_code LIKE 'platform.tenant%' OR pp.permission_code LIKE 'platform.tenants.%') LIMIT 1),
                    (SELECT pf.id FROM platform_features pf JOIN platform_modules pm ON pf.platform_module_id = pm.id WHERE (pm.module_code = 'billing_core' OR pm.module_key = 'billing_core') AND (pp.permission_code LIKE 'platform.subscription%' OR pp.permission_code LIKE 'platform.billing%') LIMIT 1),
                    (SELECT pf.id FROM platform_features pf JOIN platform_modules pm ON pf.platform_module_id = pm.id WHERE (pm.module_code = 'user_management' OR pm.module_key = 'user_management') AND pp.permission_code LIKE 'platform.user%' LIMIT 1),
                    (SELECT pf.id FROM platform_features pf JOIN platform_modules pm ON pf.platform_module_id = pm.id WHERE (pm.module_code = 'role_permission_management' OR pm.module_key = 'role_permission_management') AND (pp.permission_code LIKE 'platform.role%' OR pp.permission_code LIKE 'platform.permission%') LIMIT 1),
                    (SELECT pf.id FROM platform_features pf JOIN platform_modules pm ON pf.platform_module_id = pm.id WHERE (pm.module_code = 'audit_logging' OR pm.module_key = 'audit_logging') AND pp.permission_code LIKE 'platform.audit%' LIMIT 1),
                    (SELECT pf.id FROM platform_features pf JOIN platform_modules pm ON pf.platform_module_id = pm.id WHERE (pm.module_code = 'integration_core' OR pm.module_key = 'integration_core') AND pp.permission_code LIKE 'platform.integration%' LIMIT 1),
                    (SELECT id FROM platform_features WHERE scope = 'PLATFORM' LIMIT 1),
                    (SELECT id FROM platform_features LIMIT 1),
                    '21000000-0000-0000-0000-000000000001'::uuid);

            -- 4. Alter columns to NOT NULL after backfill completes
            ALTER TABLE platform_permissions
            ALTER COLUMN platform_module_id SET NOT NULL,
            ALTER COLUMN platform_feature_id SET NOT NULL;

            -- 5. Add Foreign Key constraints
            ALTER TABLE platform_permissions
            DROP CONSTRAINT IF EXISTS fk_platform_permissions_platform_module_id_platform_modules;
            ALTER TABLE platform_permissions
            ADD CONSTRAINT fk_platform_permissions_platform_module_id_platform_modules
            FOREIGN KEY (platform_module_id) REFERENCES platform_modules(id) ON DELETE RESTRICT;

            ALTER TABLE platform_permissions
            DROP CONSTRAINT IF EXISTS fk_platform_permissions_platform_feature_id_platform_features;
            ALTER TABLE platform_permissions
            ADD CONSTRAINT fk_platform_permissions_platform_feature_id_platform_features
            FOREIGN KEY (platform_feature_id) REFERENCES platform_features(id) ON DELETE RESTRICT;

            -- 6. Add DB CHECK constraints for explicit scope
            ALTER TABLE platform_modules
            DROP CONSTRAINT IF EXISTS ck_platform_modules_scope;
            ALTER TABLE platform_modules
            ADD CONSTRAINT ck_platform_modules_scope CHECK (scope IN ('PLATFORM', 'TENANT'));

            ALTER TABLE platform_features
            DROP CONSTRAINT IF EXISTS ck_platform_features_scope;
            ALTER TABLE platform_features
            ADD CONSTRAINT ck_platform_features_scope CHECK (scope IN ('PLATFORM', 'TENANT'));

            ALTER TABLE permission_definitions
            DROP CONSTRAINT IF EXISTS ck_permission_definitions_scope;
            ALTER TABLE permission_definitions
            ADD CONSTRAINT ck_permission_definitions_scope CHECK (scope IN ('PLATFORM', 'TENANT'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE permission_definitions DROP CONSTRAINT IF EXISTS ck_permission_definitions_scope;
            ALTER TABLE platform_features DROP CONSTRAINT IF EXISTS ck_platform_features_scope;
            ALTER TABLE platform_modules DROP CONSTRAINT IF EXISTS ck_platform_modules_scope;

            ALTER TABLE platform_permissions DROP CONSTRAINT IF EXISTS fk_platform_permissions_platform_feature_id_platform_features;
            ALTER TABLE platform_permissions DROP CONSTRAINT IF EXISTS fk_platform_permissions_platform_module_id_platform_modules;

            ALTER TABLE platform_permissions DROP COLUMN IF EXISTS platform_feature_id;
            ALTER TABLE platform_permissions DROP COLUMN IF EXISTS platform_module_id;
            """);
    }
}
