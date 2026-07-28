using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs polluted <c>tenants.status</c> values to the approved lifecycle set
/// before <see cref="AddTenantLifecycleStatusCheckConstraint"/> is applied.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260727150000_RepairTenantLifecycleStatusData")]
public partial class RepairTenantLifecycleStatusData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fail safely on unexpected legacy values (never silent DRAFT/ACTIVE).
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                unknown_count integer;
                unknown_sample text;
            BEGIN
                SELECT COUNT(*), string_agg(DISTINCT status, ', ' ORDER BY status)
                INTO unknown_count, unknown_sample
                FROM tenants
                WHERE lower(trim(status)) NOT IN (
                    'draft',
                    'pending_payment',
                    'pending_activation',
                    'active',
                    'suspended',
                    'cancelled',
                    'canceled',
                    'pending',
                    'unpaid',
                    'overdue',
                    'failed',
                    'paid',
                    'verified',
                    'waived',
                    'setup_pending',
                    'inactive'
                );

                IF unknown_count > 0 THEN
                    RAISE EXCEPTION
                        'RepairTenantLifecycleStatusData aborted: % tenant(s) have unexpected status values: %',
                        unknown_count,
                        unknown_sample;
                END IF;
            END $$;
            """);

        migrationBuilder.Sql(
            """
            UPDATE tenants
            SET status = CASE
                -- 1. Explicit cancelled / suspended
                WHEN lower(trim(status)) IN ('cancelled', 'canceled') THEN 'cancelled'
                WHEN lower(trim(status)) = 'suspended' THEN 'suspended'

                -- 2. Already approved lifecycle (normalize casing)
                WHEN lower(trim(status)) = 'draft' THEN 'draft'
                WHEN lower(trim(status)) = 'pending_payment' THEN 'pending_payment'
                WHEN lower(trim(status)) = 'pending_activation' THEN 'pending_activation'
                WHEN lower(trim(status)) = 'active' THEN 'active'

                -- 3. Activation evidence takes priority over billing labels
                WHEN activated_at IS NOT NULL
                     AND lower(trim(status)) IN ('inactive', 'paid', 'verified', 'waived', 'setup_pending')
                     AND lower(trim(status)) = 'inactive' THEN 'suspended'
                WHEN activated_at IS NOT NULL
                     AND lower(trim(status)) IN ('paid', 'verified', 'waived', 'setup_pending') THEN 'active'

                -- 4. Approved legacy billing / setup mappings
                WHEN lower(trim(status)) IN ('pending', 'unpaid', 'overdue', 'failed') THEN 'pending_payment'
                WHEN lower(trim(status)) IN ('paid', 'verified', 'waived') THEN 'pending_activation'
                WHEN lower(trim(status)) = 'setup_pending' THEN 'active'
                WHEN lower(trim(status)) = 'inactive' AND activated_at IS NOT NULL THEN 'suspended'
                WHEN lower(trim(status)) = 'inactive' THEN 'draft'

                ELSE status
            END,
            updated_at = CURRENT_TIMESTAMP
            WHERE lower(trim(status)) IS DISTINCT FROM CASE
                WHEN lower(trim(status)) IN ('cancelled', 'canceled') THEN 'cancelled'
                WHEN lower(trim(status)) = 'suspended' THEN 'suspended'
                WHEN lower(trim(status)) = 'draft' THEN 'draft'
                WHEN lower(trim(status)) = 'pending_payment' THEN 'pending_payment'
                WHEN lower(trim(status)) = 'pending_activation' THEN 'pending_activation'
                WHEN lower(trim(status)) = 'active' THEN 'active'
                WHEN activated_at IS NOT NULL
                     AND lower(trim(status)) = 'inactive' THEN 'suspended'
                WHEN activated_at IS NOT NULL
                     AND lower(trim(status)) IN ('paid', 'verified', 'waived', 'setup_pending') THEN 'active'
                WHEN lower(trim(status)) IN ('pending', 'unpaid', 'overdue', 'failed') THEN 'pending_payment'
                WHEN lower(trim(status)) IN ('paid', 'verified', 'waived') THEN 'pending_activation'
                WHEN lower(trim(status)) = 'setup_pending' THEN 'active'
                WHEN lower(trim(status)) = 'inactive' THEN 'draft'
                ELSE lower(trim(status))
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Irreversible: original polluted billing/setup labels cannot be reconstructed
        // from approved lifecycle values alone.
        migrationBuilder.Sql(
            """
            -- Down is intentionally a no-op.
            -- RepairTenantLifecycleStatusData cannot restore prior polluted tenants.status values.
            SELECT 1;
            """);
    }
}
