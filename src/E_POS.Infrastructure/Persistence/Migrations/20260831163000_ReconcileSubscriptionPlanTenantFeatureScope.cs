using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Reconciles subscription plan and tenant feature entitlement scope integrity.
/// Tenant subscription plans must only contain features with scope = 'TENANT'.
/// Restores canonical PLATFORM scope for user_accounts feature and removes any invalid
/// subscription_plan_features and tenant_feature_entitlements rows referencing PLATFORM features.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260831163000_ReconcileSubscriptionPlanTenantFeatureScope")]
public partial class ReconcileSubscriptionPlanTenantFeatureScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Restore canonical scope = 'PLATFORM' for user_accounts feature
        migrationBuilder.Sql("""
            UPDATE platform_features
            SET scope = 'PLATFORM',
                updated_at = now()
            WHERE feature_code = 'user_accounts';
            """);

        // 2. Remove subscription_plan_features rows pointing to PLATFORM-scoped features
        migrationBuilder.Sql("""
            DELETE FROM subscription_plan_features
            WHERE platform_feature_id IN (
                SELECT id FROM platform_features WHERE scope = 'PLATFORM'
            );
            """);

        // 3. Remove tenant_feature_entitlements rows pointing to PLATFORM-scoped features
        migrationBuilder.Sql("""
            DELETE FROM tenant_feature_entitlements
            WHERE COALESCE(platform_feature_id, feature_id) IN (
                SELECT id FROM platform_features WHERE scope = 'PLATFORM'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Non-destructive: preserve canonical feature scopes on rollback.
    }
}
