using System.Globalization;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class TenantPermissionSeedSqlBuilder
{
    public static string BuildPermissionUpsertSql(IReadOnlyList<TenantPermissionSeedDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            return string.Empty;
        }

        var rows = string.Join(
            $",{Environment.NewLine}",
            definitions.Select(FormatPermissionInsertRow));

        return $$"""
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
                updated_at)
            VALUES
            {{rows}}
            ON CONFLICT (permission_code) DO UPDATE
            SET module_id = EXCLUDED.module_id,
                feature_id = EXCLUDED.feature_id,
                action_type = EXCLUDED.action_type,
                description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                updated_at = now();
            """;
    }

    public static string BuildPermissionDeleteSql(IReadOnlyList<TenantPermissionSeedDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            return string.Empty;
        }

        return $$"""
            DELETE FROM permission_definitions
            WHERE permission_code IN ({{FormatPermissionCodeInList(definitions.Select(static definition => definition.PermissionCode))}});
            """;
    }

    public static string BuildCashierRoleAssignmentUpsertSql(IReadOnlyList<string> permissionCodes)
    {
        if (permissionCodes.Count == 0)
        {
            return string.Empty;
        }

        return $$"""
            INSERT INTO tenant_role_permissions (
                id,
                tenant_id,
                role_id,
                permission_id,
                granted_by_tenant_user_id,
                granted_at,
                notes,
                created_at)
            SELECT
                gen_random_uuid(),
                '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
                '{{DevelopmentTenantSeedConstants.CashierRoleId}}',
                permission_definitions.id,
                NULL,
                now(),
                'Development cashier POS permission seed.',
                now()
            FROM permission_definitions
            WHERE permission_definitions.permission_code IN ({{FormatPermissionCodeInList(permissionCodes)}})
            ON CONFLICT (tenant_id, role_id, permission_id) DO UPDATE
            SET revoked_at = NULL,
                revoked_by_tenant_user_id = NULL,
                notes = EXCLUDED.notes;
            """;
    }

    public static string BuildCashierRoleAssignmentDeleteSql(IReadOnlyList<string> permissionCodes)
    {
        if (permissionCodes.Count == 0)
        {
            return string.Empty;
        }

        return $$"""
            DELETE FROM tenant_role_permissions
            USING permission_definitions
            WHERE tenant_role_permissions.permission_id = permission_definitions.id
              AND tenant_role_permissions.tenant_id = '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}'
              AND tenant_role_permissions.role_id = '{{DevelopmentTenantSeedConstants.CashierRoleId}}'
              AND permission_definitions.permission_code IN ({{FormatPermissionCodeInList(permissionCodes)}});
            """;
    }

    private static string FormatPermissionInsertRow(TenantPermissionSeedDefinition definition)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"('{definition.Id}', '{EscapeSql(definition.PermissionCode)}', '{definition.ModuleId}', '{definition.FeatureId}', '{EscapeSql(definition.ActionType)}', '{EscapeSql(definition.Description)}', TRUE, TRUE, now(), now())");
    }

    private static string FormatPermissionCodeInList(IEnumerable<string> permissionCodes)
    {
        return string.Join(
            ", ",
            permissionCodes
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static code => code, StringComparer.Ordinal)
                .Select(static code => $"'{EscapeSql(code)}'"));
    }

    private static string EscapeSql(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
