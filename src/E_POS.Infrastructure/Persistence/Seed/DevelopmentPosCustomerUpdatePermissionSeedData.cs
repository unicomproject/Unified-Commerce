using E_POS.Domain.Modules.Tenant.AccessControl.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seed SQL for <c>customers.update</c> only.
/// Deterministic ID must not collide with sales.checkout (77777777-0316-...).
/// </summary>
public static class DevelopmentPosCustomerUpdatePermissionSeedData
{
    public static readonly Guid PermissionId = Guid.Parse("77777777-0338-4000-8000-000000000001");
    public const string PermissionCode = CustomerPermissions.Update;

    public static TenantPermissionSeedDefinition Definition { get; } = new(
        PermissionId,
        PermissionCode,
        DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
        DevelopmentPosPermissionCatalogSeedConstants.PosCustomersFeatureId,
        "update",
        "Update customers on POS.");

    public static string UpSql => $$"""
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM permission_definitions
                WHERE id = '{{PermissionId}}'::uuid
                  AND permission_code <> '{{PermissionCode}}'
            ) THEN
                RAISE EXCEPTION
                    'customers.update seed UUID {{PermissionId}} is already owned by another permission';
            END IF;
        END $$;

        UPDATE permission_definitions
        SET
            module_id = '{{DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId}}'::uuid,
            feature_id = '{{DevelopmentPosPermissionCatalogSeedConstants.PosCustomersFeatureId}}'::uuid,
            action_type = 'update',
            description = 'Update customers on POS.',
            is_system = TRUE,
            is_active = TRUE,
            updated_at = now()
        WHERE permission_code = '{{PermissionCode}}';

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
            '{{PermissionId}}'::uuid,
            '{{PermissionCode}}',
            '{{DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId}}'::uuid,
            '{{DevelopmentPosPermissionCatalogSeedConstants.PosCustomersFeatureId}}'::uuid,
            'update',
            'Update customers on POS.',
            TRUE,
            TRUE,
            now(),
            now()
        WHERE NOT EXISTS (
            SELECT 1
            FROM permission_definitions
            WHERE permission_code = '{{PermissionCode}}'
        );
        """;

    public static string DownSql => $$"""
        DELETE FROM permission_definitions
        WHERE id = '{{PermissionId}}'::uuid
          AND permission_code = '{{PermissionCode}}';
        """;

    public static string CashierAssignmentUpSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentUpsertSql([PermissionCode]);

    public static string CashierAssignmentDownSql =>
        TenantPermissionSeedSqlBuilder.BuildCashierRoleAssignmentDeleteSql([PermissionCode]);
}
