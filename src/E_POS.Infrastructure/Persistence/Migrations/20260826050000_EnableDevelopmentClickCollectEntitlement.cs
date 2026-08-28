using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260826050000_EnableDevelopmentClickCollectEntitlement")]
public sealed class EnableDevelopmentClickCollectEntitlement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO tenant_feature_entitlements (
                id,
                tenant_id,
                platform_feature_id,
                feature_id,
                entitlement_status,
                source_type,
                is_enabled,
                effective_from,
                effective_until,
                created_at,
                updated_at
            )
            SELECT
                md5(tenants.id::text || ':' || platform_features.id::text)::uuid,
                tenants.id,
                platform_features.id,
                platform_features.id,
                'ENABLED',
                'MANUAL',
                true,
                now(),
                NULL,
                now(),
                now()
            FROM tenants
            CROSS JOIN platform_features
            WHERE tenants.id = '55555555-0000-4000-8000-000000000001'::uuid
              AND platform_features.feature_code = 'click_collect'
            ON CONFLICT (tenant_id, platform_feature_id) DO UPDATE
            SET entitlement_status = 'ENABLED',
                is_enabled = true,
                effective_until = NULL,
                revoked_at = NULL,
                revoked_by_platform_user_id = NULL,
                revoked_reason = NULL,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Development acceptance data is intentionally preserved on rollback.
    }
}
