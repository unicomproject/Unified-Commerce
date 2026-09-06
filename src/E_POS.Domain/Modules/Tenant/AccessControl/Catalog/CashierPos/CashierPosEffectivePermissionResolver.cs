using System.Collections.ObjectModel;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

/// <summary>
/// Chunk 5 authoritative effective-permission resolver.
/// Pure in-memory evaluation over a candidate grant set.
/// Does not query the database and does not auto-grant children from parents.
/// </summary>
public static class CashierPosEffectivePermissionResolver
{
    private static readonly Lazy<IReadOnlyDictionary<string, CashierPosPermissionDefinition>> AllByCode =
        new(() => new ReadOnlyDictionary<string, CashierPosPermissionDefinition>(
            CashierPosCanonicalPermissionCatalog.All
                .ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase)));

    private static readonly Lazy<IReadOnlyDictionary<string, string>> ParentByChild =
        new(() => new ReadOnlyDictionary<string, string>(
            CashierPosCanonicalPermissionCatalog.RoleAssignable
                .Where(d => !string.IsNullOrWhiteSpace(d.ParentCode))
                .ToDictionary(
                    d => d.Code,
                    d => d.ParentCode!,
                    StringComparer.OrdinalIgnoreCase)));

    /// <summary>
    /// Resolves the effective permission set from candidate grants.
    /// Candidate set is expected to already be the grant-only union of active
    /// role/user/outlet assignments with inactive definitions removed.
    /// </summary>
    public static IReadOnlyList<string> Resolve(IEnumerable<string>? candidatePermissionCodes)
    {
        var candidates = NormalizeCandidates(candidatePermissionCodes);
        if (candidates.Count == 0)
        {
            return Array.Empty<string>();
        }

        var memo = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var effective = new List<string>(candidates.Count);

        foreach (var code in candidates.OrderBy(static c => c, StringComparer.OrdinalIgnoreCase))
        {
            if (IsEffective(code, candidates, memo, visiting))
            {
                effective.Add(code);
            }
        }

        return effective;
    }

    public static bool IsEffective(
        string permissionCode,
        IReadOnlySet<string> candidateSet)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var memo = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return IsEffective(permissionCode.Trim(), candidateSet, memo, visiting);
    }

    private static bool IsEffective(
        string code,
        IReadOnlySet<string> candidates,
        Dictionary<string, bool> memo,
        HashSet<string> visiting)
    {
        if (memo.TryGetValue(code, out var cached))
        {
            return cached;
        }

        if (!candidates.Contains(code))
        {
            memo[code] = false;
            return false;
        }

        if (!IsEligibleCandidate(code))
        {
            memo[code] = false;
            return false;
        }

        if (!ParentByChild.Value.TryGetValue(code, out var parentCode)
            || string.IsNullOrWhiteSpace(parentCode))
        {
            memo[code] = true;
            return true;
        }

        if (!visiting.Add(code))
        {
            // Cycle / corrupted graph → fail closed.
            memo[code] = false;
            return false;
        }

        var result = IsEffective(parentCode, candidates, memo, visiting);
        visiting.Remove(code);
        memo[code] = result;
        return result;
    }

    private static IReadOnlySet<string> NormalizeCandidates(IEnumerable<string>? codes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (codes is null)
        {
            return set;
        }

        foreach (var raw in codes)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var code = raw.Trim();
            if (!IsEligibleCandidate(code))
            {
                continue;
            }

            set.Add(code);
        }

        return set;
    }

    /// <summary>
    /// Fail-closed eligibility before parent evaluation.
    /// Non-Cashier namespaces pass through (tenant/platform/catalog/etc.).
    /// Managed Cashier namespaces must be role-assignable catalog (or known legacy alias).
    /// </summary>
    internal static bool IsEligibleCandidate(string code)
    {
        if (ContainsForbiddenToken(code))
        {
            return false;
        }

        if (code.StartsWith("pre_auth.", StringComparison.OrdinalIgnoreCase)
            || CashierPosPermissionAssignmentRules.IsPreAuth(code))
        {
            return false;
        }

        if (!CashierPosPermissionAssignmentRules.IsCashierPosManagedNamespace(code))
        {
            // tenant.*, catalog.*, inventory.*, etc. remain effective if granted.
            return true;
        }

        if (AllByCode.Value.TryGetValue(code, out var definition))
        {
            if (definition.Kind == CashierPosPermissionDefinitionKind.PreAuth
                || !definition.IsRoleAssignable)
            {
                return false;
            }

            return CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(code);
        }

        if (CashierPosPermissionAssignmentRules.IsKnownLegacyPosAlias(code))
        {
            return true;
        }

        // Unknown managed four-tier or incomplete shorthand → fail closed.
        return false;
    }

    private static bool ContainsForbiddenToken(string code) =>
        code.Contains('*', StringComparison.Ordinal)
        || code.Contains('/', StringComparison.Ordinal)
        || code.Contains(' ', StringComparison.Ordinal)
        || code.Contains('\t', StringComparison.Ordinal)
        || code.Contains("..", StringComparison.Ordinal)
        || code.StartsWith(".", StringComparison.Ordinal)
        || code.EndsWith(".", StringComparison.Ordinal);
}
