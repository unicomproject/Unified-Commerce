using System.Collections.ObjectModel;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

/// <summary>
/// Chunk 4 ongoing assignment rules for Cashier POS canonical permissions.
/// Does not implement effective-permission resolution.
/// </summary>
public static class CashierPosPermissionAssignmentRules
{
    public enum FailureKind
    {
        None = 0,
        InvalidFormat,
        PreAuthNotAssignable,
        UnknownCanonicalPermission,
        ParentPermissionRequired,
    }

    public sealed record ValidationFailure(
        FailureKind Kind,
        string PermissionCode,
        string? RequiredParentCode = null);

    public sealed record ValidationResult(
        bool IsValid,
        IReadOnlyList<ValidationFailure> Failures)
    {
        public static ValidationResult Success { get; } =
            new(true, Array.Empty<ValidationFailure>());

        public static ValidationResult Failed(params ValidationFailure[] failures) =>
            new(false, failures);
    }

    private static readonly Lazy<IReadOnlyDictionary<string, CashierPosPermissionDefinition>> RoleAssignableByCode =
        new(() => new ReadOnlyDictionary<string, CashierPosPermissionDefinition>(
            CashierPosCanonicalPermissionCatalog.RoleAssignable
                .ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase)));

    private static readonly Lazy<IReadOnlyDictionary<string, CashierPosPermissionDefinition>> AllByCode =
        new(() => new ReadOnlyDictionary<string, CashierPosPermissionDefinition>(
            CashierPosCanonicalPermissionCatalog.All
                .ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase)));

    private static readonly Lazy<IReadOnlySet<string>> RoleAssignableCodes =
        new(() => RoleAssignableByCode.Value.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase));

    private static readonly Lazy<IReadOnlySet<string>> KnownLegacyPosAliases =
        new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Pre-canonical POS aliases still present in cashier ceiling / seeds.
            "pos.home.view",
            "pos.dashboard.view",
            "pos.new_sale.view",
            "pos.till.open",
            "pos.till.close",
            "pos.till.view",
            "pos.hardware.settings",
            "pos.sale.create",
            "pos.sale.park",
            "pos.sale.recall",
            "pos.sale.park.view",
            "pos.discount.apply",
            "pos.refund.process",
            "pos.refund.approve",
            "pos.exchange.process",
            "pos.online_orders.manage",
            "pos.till.cash_movement",
            "till.session.view",
        });

    public static IReadOnlySet<string> RoleAssignablePermissionCodes => RoleAssignableCodes.Value;

    public static bool IsKnownLegacyPosAlias(string permissionCode) =>
        KnownLegacyPosAliases.Value.Contains(permissionCode);

    public static bool IsCashierPosManagedNamespace(string? permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        return permissionCode.StartsWith("pos.", StringComparison.OrdinalIgnoreCase)
            || permissionCode.StartsWith("commerce.", StringComparison.OrdinalIgnoreCase)
            || permissionCode.StartsWith("pre_auth.", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPreAuth(string? permissionCode) =>
        !string.IsNullOrWhiteSpace(permissionCode)
        && AllByCode.Value.TryGetValue(permissionCode.Trim(), out var definition)
        && definition.Kind == CashierPosPermissionDefinitionKind.PreAuth;

    public static bool TryGetDefinition(string permissionCode, out CashierPosPermissionDefinition definition) =>
        AllByCode.Value.TryGetValue(permissionCode, out definition!);

    public static bool TryGetRoleAssignable(string permissionCode, out CashierPosPermissionDefinition definition) =>
        RoleAssignableByCode.Value.TryGetValue(permissionCode, out definition!);

    public static bool TryGetParentCode(string permissionCode, out string? parentCode)
    {
        parentCode = null;
        if (!RoleAssignableByCode.Value.TryGetValue(permissionCode, out var definition))
        {
            return false;
        }

        parentCode = definition.ParentCode;
        return parentCode is not null;
    }

    /// <summary>
    /// Validates a target assignment set for ongoing configuration.
    /// Parent rule (OPTION A for new grants): a newly introduced child requires its parent
    /// to be present in <paramref name="availableParentCodes"/>. Existing orphaned children
    /// already in <paramref name="currentlyGrantedCodes"/> may remain after parent revoke.
    /// </summary>
    public static ValidationResult ValidateAssignmentSet(
        IEnumerable<string> targetPermissionCodes,
        IEnumerable<string>? currentlyGrantedCodes = null,
        IEnumerable<string>? availableParentCodes = null)
    {
        var target = Normalize(targetPermissionCodes);
        var currentlyGranted = Normalize(currentlyGrantedCodes ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var availableParents = Normalize(availableParentCodes ?? target)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<ValidationFailure>();

        foreach (var code in target)
        {
            if (ContainsForbiddenToken(code))
            {
                failures.Add(new ValidationFailure(FailureKind.InvalidFormat, code));
                continue;
            }

            if (IsPreAuth(code)
                || code.StartsWith("pre_auth.", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(new ValidationFailure(FailureKind.PreAuthNotAssignable, code));
                continue;
            }

            if (IsCashierPosManagedNamespace(code))
            {
                if (AllByCode.Value.TryGetValue(code, out var anyDefinition))
                {
                    if (!anyDefinition.IsRoleAssignable
                        || anyDefinition.Kind == CashierPosPermissionDefinitionKind.PreAuth)
                    {
                        failures.Add(new ValidationFailure(FailureKind.PreAuthNotAssignable, code));
                        continue;
                    }

                    if (!CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(code))
                    {
                        failures.Add(new ValidationFailure(FailureKind.InvalidFormat, code));
                        continue;
                    }
                }
                else if (CashierPosPermissionCodeTaxonomy.IsValidFourTierCanonicalCode(code))
                {
                    // Four-tier managed code not in approved catalog.
                    failures.Add(new ValidationFailure(FailureKind.UnknownCanonicalPermission, code));
                    continue;
                }
                else if (!IsKnownLegacyPosAlias(code))
                {
                    // Incomplete managed namespace codes (e.g. pos.payments.cash) are rejected.
                    failures.Add(new ValidationFailure(FailureKind.InvalidFormat, code));
                    continue;
                }
            }

            if (TryGetParentCode(code, out var parentCode)
                && parentCode is not null
                && !availableParents.Contains(parentCode)
                && !currentlyGranted.Contains(code))
            {
                failures.Add(new ValidationFailure(
                    FailureKind.ParentPermissionRequired,
                    code,
                    parentCode));
            }
        }

        return failures.Count == 0
            ? ValidationResult.Success
            : ValidationResult.Failed(failures.ToArray());
    }

    private static bool ContainsForbiddenToken(string code) =>
        code.Contains('*', StringComparison.Ordinal)
        || code.Contains('/', StringComparison.Ordinal)
        || code.Contains(' ', StringComparison.Ordinal)
        || code.Contains('\t', StringComparison.Ordinal)
        || code.Contains("..", StringComparison.Ordinal)
        || code.StartsWith(".", StringComparison.Ordinal)
        || code.EndsWith(".", StringComparison.Ordinal);

    private static IReadOnlyList<string> Normalize(IEnumerable<string> codes) =>
        codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
