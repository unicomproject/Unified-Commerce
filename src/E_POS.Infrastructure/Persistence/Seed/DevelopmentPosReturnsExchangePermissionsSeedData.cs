using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds returns.create, exchanges.*, and pos.refund.approve.
/// Existing returns.view / refunds.view / refunds.create remain in
/// <see cref="DevelopmentPosPaymentReceiptPermissionsSeedData"/>.
/// </summary>
public static class DevelopmentPosReturnsExchangePermissionsSeedData
{
    private static readonly Guid ModuleId =
        DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId;

    private static readonly Guid ReturnsFeatureId =
        DevelopmentPosPermissionCatalogSeedConstants.PosReturnsFeatureId;

    private static readonly Guid ExchangesFeatureId =
        DevelopmentPosPermissionCatalogSeedConstants.PosExchangesFeatureId;

    public static IReadOnlyList<TenantPermissionSeedDefinition> Definitions { get; } =
    [
        new(
            Guid.Parse("77777777-0340-4000-8000-000000000001"),
            ReturnsPermissions.CreateReturn,
            ModuleId,
            ReturnsFeatureId,
            "create_return",
            "Create POS returns."),
        new(
            Guid.Parse("77777777-0341-4000-8000-000000000001"),
            ReturnsPermissions.ViewExchanges,
            ModuleId,
            ExchangesFeatureId,
            "view_exchanges",
            "View POS exchanges."),
        new(
            Guid.Parse("77777777-0342-4000-8000-000000000001"),
            ReturnsPermissions.CreateExchange,
            ModuleId,
            ExchangesFeatureId,
            "create_exchange",
            "Create POS exchanges."),
        new(
            Guid.Parse("77777777-0343-4000-8000-000000000001"),
            ReturnsPermissions.ApproveRefund,
            ModuleId,
            ReturnsFeatureId,
            "approve_refund",
            "Approve POS refunds that require manager authority."),
    ];

    /// <summary>
    /// Cashier-default codes. Does not include <see cref="ReturnsPermissions.ApproveRefund"/>.
    /// </summary>
    public static IReadOnlyList<string> CashierPermissionCodes { get; } =
    [
        ReturnsPermissions.CreateReturn,
        ReturnsPermissions.ViewExchanges,
        ReturnsPermissions.CreateExchange,
    ];

    /// <summary>
    /// Full returns/refunds/exchanges set including approval (managers/admins).
    /// </summary>
    public static IReadOnlyList<string> ManagerPermissionCodes { get; } =
    [
        ReturnsPermissions.ViewReturns,
        ReturnsPermissions.CreateReturn,
        ReturnsPermissions.ViewRefunds,
        ReturnsPermissions.CreateRefund,
        ReturnsPermissions.ViewExchanges,
        ReturnsPermissions.CreateExchange,
        ReturnsPermissions.ApproveRefund,
    ];

    public static string FeatureUpsertSql { get; } = """
        INSERT INTO platform_features (
            id,
            platform_module_id,
            feature_code,
            feature_key,
            feature_name,
            is_core_feature,
            name,
            description,
            status,
            sort_order,
            created_at,
            updated_at)
        VALUES (
            '72000000-0000-0000-0000-000000000022',
            '71000000-0000-0000-0000-000000000010',
            'pos.exchanges',
            'pos.exchanges',
            'POS Exchanges',
            true,
            'POS Exchanges',
            'Exchange replacement and settlement on POS.',
            'ACTIVE',
            85,
            now(),
            now()
        )
        ON CONFLICT (feature_key) DO UPDATE
        SET platform_module_id = EXCLUDED.platform_module_id,
            feature_code = EXCLUDED.feature_code,
            feature_name = EXCLUDED.feature_name,
            is_core_feature = EXCLUDED.is_core_feature,
            name = EXCLUDED.name,
            description = EXCLUDED.description,
            status = 'ACTIVE',
            sort_order = EXCLUDED.sort_order,
            updated_at = now();
        """;

    public static string PermissionUpsertSql =>
        TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(Definitions);

    public static string CashierAssignmentUpsertSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql(CashierPermissionCodes);

    public static string ManagerAssignmentUpsertSql { get; } = $"""
        INSERT INTO tenant_role_permissions (
            id,
            tenant_id,
            role_id,
            permission_id,
            notes,
            created_at
        )
        SELECT
            md5(mapping.role_code || ':' || mapping.permission_code)::uuid,
            '{DevelopmentTenantSeedConstants.DevelopmentTenantId}',
            tenant_roles.id,
            permission_definitions.id,
            'POS returns/refunds/exchanges manager permission seed.',
            now()
        FROM (
            VALUES
                ('STORE_MANAGER', '{ReturnsPermissions.ViewReturns}'),
                ('STORE_MANAGER', '{ReturnsPermissions.CreateReturn}'),
                ('STORE_MANAGER', '{ReturnsPermissions.ViewRefunds}'),
                ('STORE_MANAGER', '{ReturnsPermissions.CreateRefund}'),
                ('STORE_MANAGER', '{ReturnsPermissions.ViewExchanges}'),
                ('STORE_MANAGER', '{ReturnsPermissions.CreateExchange}'),
                ('STORE_MANAGER', '{ReturnsPermissions.ApproveRefund}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.ViewReturns}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.CreateReturn}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.ViewRefunds}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.CreateRefund}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.ViewExchanges}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.CreateExchange}'),
                ('TENANT_ADMIN', '{ReturnsPermissions.ApproveRefund}')
        ) AS mapping(role_code, permission_code)
        JOIN tenant_roles
            ON tenant_roles.role_code = mapping.role_code
           AND tenant_roles.tenant_id = '{DevelopmentTenantSeedConstants.DevelopmentTenantId}'
        JOIN permission_definitions
            ON permission_definitions.permission_code = mapping.permission_code
        ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
        """;

    public static string UpSql =>
        FeatureUpsertSql
        + Environment.NewLine
        + PermissionUpsertSql
        + Environment.NewLine
        + CashierAssignmentUpsertSql
        + Environment.NewLine
        + ManagerAssignmentUpsertSql;

    public static string DownSql => $"""
        DELETE FROM tenant_role_permissions
        USING permission_definitions
        WHERE tenant_role_permissions.permission_id = permission_definitions.id
          AND permission_definitions.permission_code IN (
            '{ReturnsPermissions.CreateReturn}',
            '{ReturnsPermissions.ViewExchanges}',
            '{ReturnsPermissions.CreateExchange}',
            '{ReturnsPermissions.ApproveRefund}'
          );

        {TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(Definitions)}

        DELETE FROM platform_features
        WHERE id = '72000000-0000-0000-0000-000000000022'
           OR feature_key = 'pos.exchanges';
        """;
}
