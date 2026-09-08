using System.Globalization;
using System.Text;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Chunk 3: idempotent seed of role-assignable Cashier POS canonical permissions
/// from <see cref="CashierPosCanonicalPermissionCatalog"/> plus tenant-safe
/// parent→child compatibility backfill. No runtime authorization changes.
/// </summary>
public static class CashierPosChunk3PermissionSeedData
{
    public const string CompatibilityRoleNotes = "Chunk 3 parent→child compatibility backfill.";
    public const string PermissionIdPrefix = "cashier-pos-chunk3-permission:";
    public const string CompatRoleIdPrefix = "chunk3-compat-role:";
    public const string CompatUserIdPrefix = "chunk3-compat-user:";

    private static readonly Lazy<SeedPayload> Payload = new(BuildPayload);

    public static IReadOnlyList<CashierPosPermissionDefinition> RoleAssignableDefinitions =>
        Payload.Value.RoleAssignable;

    public static IReadOnlyList<CashierPosPermissionDefinition> FineGrainedDefinitions =>
        Payload.Value.FineGrained;

    public static IReadOnlyList<(string ParentCode, string ChildCode)> CompatibilityPairs =>
        Payload.Value.CompatibilityPairs;

    public static int ExistingCatalogCount =>
        Payload.Value.RoleAssignable.Count(d => d.Kind == CashierPosPermissionDefinitionKind.Existing);

    public static int FineGrainedCount => Payload.Value.FineGrained.Count;

    public static int PreAuthExcludedCount =>
        CashierPosCanonicalPermissionCatalog.All.Count(
            d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth);

    public static string DefinitionUpsertSql => Payload.Value.DefinitionUpsertSql;

    public static string CompatibilityBackfillSql => Payload.Value.CompatibilityBackfillSql;

    public static string FineGrainedAssignmentDownSql => Payload.Value.FineGrainedAssignmentDownSql;

    public static string FineGrainedDefinitionDownSql => Payload.Value.FineGrainedDefinitionDownSql;

    public static string UpSql =>
        DefinitionUpsertSql
        + Environment.NewLine
        + CompatibilityBackfillSql;

    public static string DownSql =>
        FineGrainedAssignmentDownSql
        + Environment.NewLine
        + FineGrainedDefinitionDownSql;

    private static SeedPayload BuildPayload()
    {
        CashierPosPermissionCodeTaxonomy.EnsureAllRoleAssignableCodesAreValid();

        var roleAssignable = CashierPosCanonicalPermissionCatalog.RoleAssignable
            .OrderBy(d => d.Code, StringComparer.Ordinal)
            .ToList();

        var fineGrained = CashierPosCanonicalPermissionCatalog.FineGrained
            .OrderBy(d => d.Code, StringComparer.Ordinal)
            .ToList();

        if (roleAssignable.Any(d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth))
        {
            throw new InvalidOperationException(
                "Pre-auth permissions must not appear in RoleAssignable seed set.");
        }

        var byCode = CashierPosCanonicalPermissionCatalog.All
            .ToDictionary(d => d.Code, StringComparer.Ordinal);

        var pairs = CashierPosCanonicalPermissionCatalog.All
            .Where(d => d.IsRoleAssignable && d.ParentCode is not null)
            .Select(d => (ParentCode: d.ParentCode!, ChildCode: d.Code))
            .OrderBy(p => p.ParentCode, StringComparer.Ordinal)
            .ThenBy(p => p.ChildCode, StringComparer.Ordinal)
            .ToList();

        foreach (var (parent, child) in pairs)
        {
            CashierPosPermissionCodeTaxonomy.EnsureValidFourTierCanonicalCode(parent);
            CashierPosPermissionCodeTaxonomy.EnsureValidFourTierCanonicalCode(child);
            if (!byCode.ContainsKey(parent))
            {
                throw new InvalidOperationException(
                    $"Compatibility pair child '{child}' references unknown parent '{parent}'.");
            }
        }

        return new SeedPayload(
            roleAssignable,
            fineGrained,
            pairs,
            BuildDefinitionUpsertSql(roleAssignable),
            BuildCompatibilityBackfillSql(pairs),
            BuildFineGrainedAssignmentDownSql(fineGrained),
            BuildFineGrainedDefinitionDownSql(fineGrained));
    }

    private static string BuildDefinitionUpsertSql(
        IReadOnlyList<CashierPosPermissionDefinition> definitions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            INSERT INTO platform_features (
                id, platform_module_id, feature_code, feature_key, feature_name,
                is_core_feature, name, description, status, sort_order, created_at, updated_at)
            VALUES
                (md5('canonical-pos-feature:pos.online_orders')::uuid,
                 '71000000-0000-0000-0000-000000000010'::uuid,
                 'pos.online_orders', 'pos.online_orders', 'POS Online Orders', TRUE,
                 'POS Online Orders', 'POS online-order / click-collect operations.', 'ACTIVE', 95, now(), now())
            ON CONFLICT (feature_key) DO UPDATE
            SET status = 'ACTIVE', updated_at = now();
            """);

        sb.AppendLine();
        sb.AppendLine("WITH seed(permission_code, feature_key, action_type, description) AS (");
        sb.AppendLine("    VALUES");

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            CashierPosPermissionCodeTaxonomy.EnsureValidFourTierCanonicalCode(definition.Code);

            var action = definition.Code.Split('.')[^1];
            var featureKey = ResolveFeatureKey(definition.Code);
            var description = EscapeSql(
                $"Cashier POS canonical permission ({definition.Kind}): {definition.Reason}");
            var comma = i == definitions.Count - 1 ? string.Empty : ",";
            sb.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"    ('{EscapeSql(definition.Code)}', '{EscapeSql(featureKey)}', '{EscapeSql(action)}', '{description}'){comma}"));
        }

        sb.AppendLine(")");
        sb.AppendLine("""
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
            SELECT
                md5('cashier-pos-chunk3-permission:' || seed.permission_code)::uuid,
                seed.permission_code,
                platform_features.platform_module_id,
                platform_features.id,
                seed.action_type,
                seed.description,
                TRUE,
                TRUE,
                now(),
                now()
            FROM seed
            JOIN platform_features ON platform_features.feature_key = seed.feature_key
            ON CONFLICT (permission_code) DO UPDATE
            SET description = EXCLUDED.description,
                is_system = TRUE,
                is_active = TRUE,
                updated_at = now();
            """);

        return sb.ToString();
    }

    private static string BuildCompatibilityBackfillSql(
        IReadOnlyList<(string ParentCode, string ChildCode)> pairs)
    {
        if (pairs.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("WITH pairs(parent_code, child_code) AS (");
        sb.AppendLine("    VALUES");
        for (var i = 0; i < pairs.Count; i++)
        {
            var (parent, child) = pairs[i];
            var comma = i == pairs.Count - 1 ? string.Empty : ",";
            sb.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"    ('{EscapeSql(parent)}', '{EscapeSql(child)}'){comma}"));
        }

        sb.AppendLine("),");
        sb.AppendLine("""
            role_backfill AS (
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
                    md5(
                        'chunk3-compat-role:'
                        || trp.tenant_id::text
                        || ':'
                        || trp.role_id::text
                        || ':'
                        || pairs.child_code)::uuid,
                    trp.tenant_id,
                    trp.role_id,
                    child_def.id,
                    NULL,
                    now(),
                    'Chunk 3 parent→child compatibility backfill.',
                    now()
                FROM pairs
                JOIN permission_definitions parent_def
                    ON parent_def.permission_code = pairs.parent_code
                JOIN permission_definitions child_def
                    ON child_def.permission_code = pairs.child_code
                JOIN tenant_role_permissions trp
                    ON trp.permission_id = parent_def.id
                   AND trp.revoked_at IS NULL
                ON CONFLICT DO NOTHING
                RETURNING 1
            ),
            user_backfill AS (
                INSERT INTO tenant_user_permissions (
                    id,
                    tenant_id,
                    user_id,
                    permission_id,
                    assigned_by_tenant_user_id,
                    assigned_at,
                    revoked_at,
                    created_at)
                SELECT
                    md5(
                        'chunk3-compat-user:'
                        || tup.tenant_id::text
                        || ':'
                        || tup.user_id::text
                        || ':'
                        || pairs.child_code)::uuid,
                    tup.tenant_id,
                    tup.user_id,
                    child_def.id,
                    NULL,
                    now(),
                    NULL,
                    now()
                FROM pairs
                JOIN permission_definitions parent_def
                    ON parent_def.permission_code = pairs.parent_code
                JOIN permission_definitions child_def
                    ON child_def.permission_code = pairs.child_code
                JOIN tenant_user_permissions tup
                    ON tup.permission_id = parent_def.id
                   AND tup.revoked_at IS NULL
                ON CONFLICT (tenant_id, user_id, permission_id) DO NOTHING
                RETURNING 1
            )
            SELECT
                (SELECT count(*) FROM role_backfill) AS role_rows,
                (SELECT count(*) FROM user_backfill) AS user_rows;
            """);

        return sb.ToString();
    }

    private static string BuildFineGrainedAssignmentDownSql(
        IReadOnlyList<CashierPosPermissionDefinition> fineGrained)
    {
        var inList = FormatCodeInList(fineGrained.Select(d => d.Code));
        return $$"""
            DELETE FROM tenant_role_permissions
            USING permission_definitions
            WHERE tenant_role_permissions.permission_id = permission_definitions.id
              AND tenant_role_permissions.notes = '{{CompatibilityRoleNotes}}'
              AND permission_definitions.permission_code IN ({{inList}});

            DELETE FROM tenant_user_permissions
            USING permission_definitions
            WHERE tenant_user_permissions.permission_id = permission_definitions.id
              AND permission_definitions.permission_code IN ({{inList}})
              AND tenant_user_permissions.id = md5(
                    'chunk3-compat-user:'
                    || tenant_user_permissions.tenant_id::text
                    || ':'
                    || tenant_user_permissions.user_id::text
                    || ':'
                    || permission_definitions.permission_code)::uuid;
            """;
    }

    private static string BuildFineGrainedDefinitionDownSql(
        IReadOnlyList<CashierPosPermissionDefinition> fineGrained)
    {
        var inList = FormatCodeInList(fineGrained.Select(d => d.Code));
        return $$"""
            DELETE FROM permission_definitions
            WHERE permission_code IN ({{inList}})
              AND id = md5('cashier-pos-chunk3-permission:' || permission_code)::uuid;
            """;
    }

    public static string ResolveFeatureKey(string permissionCode)
    {
        if (permissionCode.StartsWith("commerce.", StringComparison.Ordinal))
        {
            return "pos.online_orders";
        }

        if (permissionCode.StartsWith("pos.notifications.", StringComparison.Ordinal))
        {
            return "pos.notifications";
        }

        if (permissionCode.StartsWith("pos.customers.", StringComparison.Ordinal))
        {
            return "pos.customers";
        }

        if (permissionCode.StartsWith("pos.cash_drawer.", StringComparison.Ordinal)
            || permissionCode.StartsWith("pos.cash_movements.", StringComparison.Ordinal))
        {
            return "pos.cash_drawer";
        }

        if (permissionCode.StartsWith("pos.till.", StringComparison.Ordinal))
        {
            return "pos.till";
        }

        if (permissionCode.StartsWith("pos.receipts.", StringComparison.Ordinal)
            || permissionCode.StartsWith("pos.sale_complete.", StringComparison.Ordinal))
        {
            return "pos.receipts";
        }

        if (permissionCode.StartsWith("pos.payments.", StringComparison.Ordinal)
            || permissionCode.StartsWith("pos.cash_payment.", StringComparison.Ordinal))
        {
            return "pos.payments";
        }

        if (permissionCode.StartsWith("pos.catalog.", StringComparison.Ordinal)
            || permissionCode.StartsWith("pos.sales.catalog.", StringComparison.Ordinal))
        {
            return "pos.products";
        }

        if (permissionCode.StartsWith("pos.returns.", StringComparison.Ordinal))
        {
            return "pos.returns";
        }

        if (permissionCode.StartsWith("pos.orders.", StringComparison.Ordinal))
        {
            return "pos.orders";
        }

        if (permissionCode.StartsWith("pos.shell.", StringComparison.Ordinal)
            || permissionCode.StartsWith("pos.home.", StringComparison.Ordinal)
            || permissionCode.Equals("pos.sales.dashboard.view", StringComparison.Ordinal))
        {
            return "pos.home";
        }

        return "pos.sales";
    }

    private static string FormatCodeInList(IEnumerable<string> codes)
    {
        return string.Join(
            ", ",
            codes
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static c => c, StringComparer.Ordinal)
                .Select(static c => $"'{EscapeSql(c)}'"));
    }

    private static string EscapeSql(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private sealed record SeedPayload(
        IReadOnlyList<CashierPosPermissionDefinition> RoleAssignable,
        IReadOnlyList<CashierPosPermissionDefinition> FineGrained,
        IReadOnlyList<(string ParentCode, string ChildCode)> CompatibilityPairs,
        string DefinitionUpsertSql,
        string CompatibilityBackfillSql,
        string FineGrainedAssignmentDownSql,
        string FineGrainedDefinitionDownSql);
}
