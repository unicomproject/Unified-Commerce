using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTenantReportingPermissions : Migration
    {
        // The Reporting API enforces the codes in TenantAdminReportPermissions, but only
        // tenant.reports.sales.view was ever defined in permission_definitions. Effective
        // permissions are resolved through that table, so no user could hold the export,
        // payments, returns, tills, etc. permissions and REP-P02 export was unreachable.
        // New definitions reuse the module/feature of tenant.reports.sales.view so they sit
        // under the same entitlement ceiling. Grants: TENANT_ADMIN gets every report
        // permission except customer PII; STORE_MANAGER gets view permissions only (its
        // outlet/till access scope still limits the data); CASHIER gets none.
        public const string DefinePermissionsSql = """
            INSERT INTO permission_definitions (
                id, permission_code, module_id, feature_id, action_type,
                description, is_system, is_active, scope, created_at, updated_at)
            SELECT
                md5(seed.permission_code)::uuid,
                seed.permission_code,
                reference.module_id, reference.feature_id, seed.action_type,
                seed.description, true, true, 'TENANT', now(), now()
            FROM (
                VALUES
                    ('tenant.reports.dashboard.view', 'view', 'View the Reports Home summary cards.'),
                    ('tenant.reports.products.view', 'view', 'View product sales reports.'),
                    ('tenant.reports.payments.view', 'view', 'View payment reports.'),
                    ('tenant.reports.tax.view', 'view', 'View tax breakdown reports.'),
                    ('tenant.reports.discounts.view', 'view', 'View discount reports.'),
                    ('tenant.reports.returns.view', 'view', 'View returns and refunds reports.'),
                    ('tenant.reports.cashiers.view', 'view', 'View cashier performance reports.'),
                    ('tenant.reports.daily-sales.view', 'view', 'View and print daily sales summaries.'),
                    ('tenant.reports.outlets.view', 'view', 'View outlet performance reports.'),
                    ('tenant.reports.tills.view', 'view', 'View till and shift closing reports.'),
                    ('tenant.reports.export', 'export', 'Export report data to CSV.'),
                    ('tenant.reports.customer-pii.view', 'view', 'View customer contact details in reports.')
            ) AS seed(permission_code, action_type, description)
            CROSS JOIN LATERAL (
                SELECT module_id, feature_id
                FROM permission_definitions
                WHERE permission_code IN ('tenant.reports.sales.view', 'reports.sales.view')
                ORDER BY (permission_code = 'tenant.reports.sales.view') DESC
                LIMIT 1
            ) AS reference
            ON CONFLICT (permission_code) DO NOTHING;
            """;

        public const string BackfillGrantsSql = """
            INSERT INTO tenant_role_permissions (
                id, tenant_id, role_id, permission_id,
                granted_by_tenant_user_id, granted_at, notes, created_at)
            SELECT
                md5('reporting-release1-role:' || tr.tenant_id::text || ':' || tr.id::text || ':' || pd.permission_code)::uuid,
                tr.tenant_id, tr.id, pd.id,
                NULL, now(), 'Reporting Release 1 default grant.', now()
            FROM tenant_roles tr
            JOIN (
                VALUES
                    ('TENANT_ADMIN', 'tenant.reports.sales.view'),
                    ('TENANT_ADMIN', 'tenant.reports.dashboard.view'),
                    ('TENANT_ADMIN', 'tenant.reports.products.view'),
                    ('TENANT_ADMIN', 'tenant.reports.payments.view'),
                    ('TENANT_ADMIN', 'tenant.reports.tax.view'),
                    ('TENANT_ADMIN', 'tenant.reports.discounts.view'),
                    ('TENANT_ADMIN', 'tenant.reports.returns.view'),
                    ('TENANT_ADMIN', 'tenant.reports.cashiers.view'),
                    ('TENANT_ADMIN', 'tenant.reports.daily-sales.view'),
                    ('TENANT_ADMIN', 'tenant.reports.outlets.view'),
                    ('TENANT_ADMIN', 'tenant.reports.tills.view'),
                    ('TENANT_ADMIN', 'tenant.reports.export'),
                    ('STORE_MANAGER', 'tenant.reports.dashboard.view'),
                    ('STORE_MANAGER', 'tenant.reports.sales.view'),
                    ('STORE_MANAGER', 'tenant.reports.products.view'),
                    ('STORE_MANAGER', 'tenant.reports.payments.view'),
                    ('STORE_MANAGER', 'tenant.reports.returns.view'),
                    ('STORE_MANAGER', 'tenant.reports.daily-sales.view'),
                    ('STORE_MANAGER', 'tenant.reports.tills.view')
            ) AS mapping(role_code, permission_code) ON mapping.role_code = tr.role_code
            JOIN permission_definitions pd ON pd.permission_code = mapping.permission_code
            WHERE tr.is_active = TRUE
            ON CONFLICT DO NOTHING;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DefinePermissionsSql);
            migrationBuilder.Sql(BackfillGrantsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM tenant_role_permissions
                WHERE notes = 'Reporting Release 1 default grant.';

                DELETE FROM permission_definitions
                WHERE permission_code IN (
                    'tenant.reports.dashboard.view', 'tenant.reports.products.view', 'tenant.reports.payments.view',
                    'tenant.reports.tax.view', 'tenant.reports.discounts.view', 'tenant.reports.returns.view',
                    'tenant.reports.cashiers.view', 'tenant.reports.daily-sales.view', 'tenant.reports.outlets.view',
                    'tenant.reports.tills.view', 'tenant.reports.export', 'tenant.reports.customer-pii.view');
                """);
        }
    }
}
