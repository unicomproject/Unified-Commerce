using System;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260814090000_AddTenantAdminOnlineStoreSetupSupport")]
public partial class AddTenantAdminOnlineStoreSetupSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM tenant_domains
                    WHERE status = 'ACTIVE' AND is_primary = TRUE
                    GROUP BY tenant_id, sales_channel_id
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot add uq_tenant_domains_active_primary_storefront: duplicate active primary tenant domains exist.';
                END IF;
            END $$;
            """);

        migrationBuilder.CreateTable(
            name: "storefront_policies",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                sales_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                policy_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                content = table.Column<string>(type: "text", nullable: false),
                version = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_storefront_policies", x => x.id);
                table.CheckConstraint("ck_storefront_policies_content_non_empty", "length(btrim(content)) > 0");
                table.CheckConstraint("ck_storefront_policies_policy_type", "policy_type IN ('TERMS', 'PRIVACY', 'CANCELLATION', 'COLLECTION', 'RETURN_REFUND')");
                table.CheckConstraint("ck_storefront_policies_status", "status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED')");
                table.ForeignKey(
                    name: "fk_storefront_policies_tenant_id_tenants",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_storefront_policies_sales_channel_tenant",
                    columns: x => new { x.tenant_id, x.sales_channel_id },
                    principalTable: "sales_channels",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_storefront_policies_created_by_tenant_user",
                    column: x => x.created_by_tenant_user_id,
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_storefront_policies_updated_by_tenant_user",
                    column: x => x.updated_by_tenant_user_id,
                    principalTable: "tenant_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "uq_storefront_policies_type_version",
            table: "storefront_policies",
            columns: new[] { "tenant_id", "sales_channel_id", "policy_type", "version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uq_storefront_policies_current_published",
            table: "storefront_policies",
            columns: new[] { "tenant_id", "sales_channel_id", "policy_type" },
            unique: true,
            filter: "status = 'PUBLISHED'");

        migrationBuilder.CreateIndex(
            name: "ix_storefront_policies_created_by_tenant_user_id",
            table: "storefront_policies",
            column: "created_by_tenant_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_storefront_policies_updated_by_tenant_user_id",
            table: "storefront_policies",
            column: "updated_by_tenant_user_id");

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS uq_tenant_domains_active_primary_storefront
            ON tenant_domains (tenant_id, sales_channel_id)
            WHERE status = 'ACTIVE' AND is_primary = TRUE;
            """);

        migrationBuilder.Sql("""
            UPDATE setting_definitions
            SET default_value = '{"schemaVersion":1,"storeStatus":"DRAFT","taxDisplayMode":"MATCH_TENANT","setupEnabled":false,"storeSlug":null,"businessDisplayName":null,"storeDescription":null,"storeEmail":null,"storePhone":null,"supportTagline":null,"branding":{"logoMediaAssetId":null,"faviconMediaAssetId":null,"primaryColor":"#FF6A00","secondaryColor":"#000000"},"support":{"email":null,"phone":null,"whatsapp":null,"helpUrl":null,"contactUsEnabled":true,"supportHours":null,"businessAddress":null},"publishedAt":null}'::jsonb,
                updated_at = NOW()
            WHERE setting_key = 'online_store.defaults';

            UPDATE tenant_settings ts
            SET setting_value =
                jsonb_strip_nulls(
                    '{"schemaVersion":1,"storeStatus":"DRAFT","taxDisplayMode":"MATCH_TENANT","setupEnabled":false,"storeSlug":null,"businessDisplayName":null,"storeDescription":null,"storeEmail":null,"storePhone":null,"supportTagline":null,"branding":{"logoMediaAssetId":null,"faviconMediaAssetId":null,"primaryColor":"#FF6A00","secondaryColor":"#000000"},"support":{"email":null,"phone":null,"whatsapp":null,"helpUrl":null,"contactUsEnabled":true,"supportHours":null,"businessAddress":null},"publishedAt":null}'::jsonb
                    || COALESCE(ts.setting_value, '{}'::jsonb)
                ),
                updated_at = NOW()
            FROM setting_definitions sd
            WHERE ts.setting_definition_id = sd.id
              AND sd.setting_key = 'online_store.defaults';
            """);

        migrationBuilder.Sql("""
            INSERT INTO platform_modules (id, module_code, module_key, module_name, name, description, status, sort_order, is_core_module, created_at, updated_at)
            VALUES (
                '71000000-0000-0000-0000-000000000001'::uuid,
                'core_commerce',
                'core_commerce',
                'Core Commerce',
                'Core Commerce',
                'Core TM-EPOS commercial capabilities.',
                'ACTIVE',
                1,
                TRUE,
                NOW(),
                NOW()
            )
            ON CONFLICT (module_code) DO UPDATE
            SET module_key = EXCLUDED.module_key,
                module_name = EXCLUDED.module_name,
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = 'ACTIVE',
                sort_order = EXCLUDED.sort_order,
                is_core_module = TRUE,
                updated_at = NOW();

            INSERT INTO platform_features (id, platform_module_id, feature_code, feature_key, feature_name, name, description, status, sort_order, is_core_feature, created_at, updated_at)
            VALUES (
                '72000000-0000-0000-0000-000000000001'::uuid,
                '71000000-0000-0000-0000-000000000001'::uuid,
                'online_store',
                'online_store',
                'Online Store',
                'Online Store',
                'Enable tenant online store channel.',
                'ACTIVE',
                1,
                TRUE,
                NOW(),
                NOW()
            )
            ON CONFLICT (platform_module_id, feature_code) DO UPDATE
            SET feature_key = EXCLUDED.feature_key,
                feature_name = EXCLUDED.feature_name,
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                status = 'ACTIVE',
                sort_order = EXCLUDED.sort_order,
                is_core_feature = TRUE,
                updated_at = NOW();

            INSERT INTO platform_modules (id, module_code, module_key, module_name, name, description, status, sort_order, is_core_module, created_at, updated_at)
            SELECT DISTINCT
                permission_definitions.module_id,
                'legacy_permission_module_' || left(permission_definitions.module_id::text, 8),
                'legacy_permission_module_' || left(permission_definitions.module_id::text, 8),
                'Legacy Permission Module',
                'Legacy Permission Module',
                'Backfilled module row for legacy permission metadata.',
                'ACTIVE',
                999,
                TRUE,
                NOW(),
                NOW()
            FROM permission_definitions
            LEFT JOIN platform_modules ON platform_modules.id = permission_definitions.module_id
            WHERE platform_modules.id IS NULL
            ON CONFLICT DO NOTHING;

            INSERT INTO platform_features (id, platform_module_id, feature_code, feature_key, feature_name, name, description, status, sort_order, is_core_feature, created_at, updated_at)
            SELECT DISTINCT
                permission_definitions.feature_id,
                permission_definitions.module_id,
                'legacy_permission_feature_' || left(permission_definitions.feature_id::text, 8),
                'legacy_permission_feature_' || left(permission_definitions.feature_id::text, 8),
                'Legacy Permission Feature',
                'Legacy Permission Feature',
                'Backfilled feature row for legacy permission metadata.',
                'ACTIVE',
                999,
                TRUE,
                NOW(),
                NOW()
            FROM permission_definitions
            LEFT JOIN platform_features ON platform_features.id = permission_definitions.feature_id
            WHERE platform_features.id IS NULL
            ON CONFLICT DO NOTHING;

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
                md5('permission:' || seed.permission_code)::uuid,
                seed.permission_code,
                '71000000-0000-0000-0000-000000000001'::uuid,
                '72000000-0000-0000-0000-000000000001'::uuid,
                seed.action_type,
                seed.description,
                TRUE,
                TRUE,
                NOW(),
                NOW()
            FROM (
                VALUES
                    ('tenant.online_store.view', 'view', 'View tenant admin online store setup.'),
                    ('tenant.online_store.manage', 'manage', 'Manage tenant admin online store setup.'),
                    ('tenant.online_store.publish', 'publish', 'Publish tenant admin online store.'),
                    ('tenant.online_store.domains.manage', 'domains_manage', 'Manage online store domains and verification.'),
                    ('tenant.online_store.branding.manage', 'branding_manage', 'Manage online store branding assets and banners.'),
                    ('tenant.online_store.support.manage', 'support_manage', 'Manage online store support contact details.'),
                    ('tenant.online_store.fulfillment.manage', 'fulfillment_manage', 'Manage click and collect outlet fulfillment rules.'),
                    ('tenant.online_store.catalog.manage', 'catalog_manage', 'Manage online store product visibility.'),
                    ('tenant.online_store.policies.manage', 'policies_manage', 'Manage online store customer policies.')
            ) AS seed(permission_code, action_type, description)
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                updated_at = NOW();

            INSERT INTO tenant_role_permissions (
                id,
                tenant_id,
                role_id,
                permission_id,
                granted_by_tenant_user_id,
                granted_at,
                notes,
                created_at
            )
            SELECT
                md5(tenant_roles.id::text || ':' || permission_definitions.permission_code)::uuid,
                tenant_roles.tenant_id,
                tenant_roles.id,
                permission_definitions.id,
                NULL,
                NOW(),
                'Tenant admin online store setup permission seed.',
                NOW()
            FROM tenant_roles
            CROSS JOIN permission_definitions
            WHERE tenant_roles.role_code = 'TENANT_ADMIN'
              AND tenant_roles.tenant_id = '55555555-0000-4000-8000-000000000001'::uuid
              AND permission_definitions.permission_code IN (
                  'tenant.online_store.view',
                  'tenant.online_store.manage',
                  'tenant.online_store.publish',
                  'tenant.online_store.domains.manage',
                  'tenant.online_store.branding.manage',
                  'tenant.online_store.support.manage',
                  'tenant.online_store.fulfillment.manage',
                  'tenant.online_store.catalog.manage',
                  'tenant.online_store.policies.manage'
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM tenant_role_permissions existing_permission
                  WHERE existing_permission.tenant_id = tenant_roles.tenant_id
                    AND existing_permission.role_id = tenant_roles.id
                    AND existing_permission.permission_id = permission_definitions.id
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS uq_tenant_domains_active_primary_storefront;");
        migrationBuilder.Sql("""
            DELETE FROM tenant_role_permissions
            USING permission_definitions
            WHERE tenant_role_permissions.permission_id = permission_definitions.id
              AND permission_definitions.permission_code IN (
                  'tenant.online_store.view',
                  'tenant.online_store.manage',
                  'tenant.online_store.publish',
                  'tenant.online_store.domains.manage',
                  'tenant.online_store.branding.manage',
                  'tenant.online_store.support.manage',
                  'tenant.online_store.fulfillment.manage',
                  'tenant.online_store.catalog.manage',
                  'tenant.online_store.policies.manage'
              );

            DELETE FROM permission_definitions
            WHERE permission_code IN (
                'tenant.online_store.view',
                'tenant.online_store.manage',
                'tenant.online_store.publish',
                'tenant.online_store.domains.manage',
                'tenant.online_store.branding.manage',
                'tenant.online_store.support.manage',
                'tenant.online_store.fulfillment.manage',
                'tenant.online_store.catalog.manage',
                'tenant.online_store.policies.manage'
            );
            """);
        migrationBuilder.DropTable(name: "storefront_policies");
    }
}
