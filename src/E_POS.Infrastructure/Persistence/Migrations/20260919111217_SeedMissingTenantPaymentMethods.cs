using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMissingTenantPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill for tenants created (or entitlement-updated) before payment method
            // seeding was wired into every tenant-creation path. Without this, checkout
            // shows "No available payment methods" because payment_methods has zero rows
            // for the tenant. Idempotent: ON CONFLICT matches the existing unique index on
            // (tenant_id, method_code), so re-running this migration is always a no-op.
            migrationBuilder.Sql("""
                INSERT INTO payment_methods (
                    id, tenant_id, method_code, method_name, method_type,
                    is_active_for_pos, is_active_for_online, requires_manual_confirmation,
                    supports_refund, requires_reference, allows_change, sort_order,
                    status, created_by_tenant_user_id, updated_by_tenant_user_id,
                    created_at, updated_at)
                SELECT
                    gen_random_uuid(), t.id, m.method_code, m.method_name, m.method_type,
                    TRUE, FALSE, FALSE, TRUE, FALSE, m.allows_change, m.sort_order,
                    'ACTIVE', NULL, NULL, now(), now()
                FROM tenants t
                CROSS JOIN (VALUES
                    ('CASH', 'Cash', 'CASH', TRUE, 1),
                    ('CARD', 'Card', 'CARD', FALSE, 2),
                    ('LANKA_QR', 'LankaQR', 'QR', FALSE, 3)
                ) AS m(method_code, method_name, method_type, allows_change, sort_order)
                ON CONFLICT (tenant_id, method_code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: this migration only backfills rows that should
            // have existed since tenant creation. Rolling it back would silently break
            // checkout for every tenant it fixed. Remove specific tenants' payment methods
            // manually if a rollback is genuinely required.
        }
    }
}
