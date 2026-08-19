using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Strategy C: do not invent subscription plan feature entitlements.
/// Plans are custom feature bundles; there is no proven global mandatory feature set.
///
/// This migration only retires ACTIVE plans that have:
/// - zero included feature mappings, AND
/// - zero tenant subscription assignments.
///
/// ACTIVE empty plans that still have tenants remain ACTIVE but are excluded from
/// Create Tenant create-options (see PlatformTenantRepository.GetCreateOptionsAsync).
/// Platform Admins must configure features intentionally before those plans are usable
/// for new tenant creation (or archive/recreate via supported lifecycle actions).
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260813120000_RepairActiveSubscriptionPlansMissingIncludedFeatures")]
public partial class RepairActiveSubscriptionPlansMissingIncludedFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Do NOT insert invented subscription_plan_features rows.
            -- Retire only orphan ACTIVE plans that cannot be safely reconstructed.
            UPDATE subscription_plans sp
            SET status = 'retired',
                updated_at = now()
            WHERE sp.status = 'active'
              AND NOT EXISTS (
                  SELECT 1
                  FROM subscription_plan_features spf
                  WHERE spf.subscription_plan_id = sp.id
                    AND spf.status = 'included'
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM tenant_subscriptions ts
                  WHERE ts.subscription_plan_id = sp.id
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Non-destructive: do not reactivate retired orphan plans on rollback.
    }
}
