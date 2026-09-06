using System.Text.RegularExpressions;

namespace E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

/// <summary>
/// Strict four-tier permission taxonomy validation (ADR_007 / Permission_Code_List).
/// Chunk 3 seed fails fast when any role-assignable code violates this contract.
/// </summary>
public static partial class CashierPosPermissionCodeTaxonomy
{
    /// <summary>
    /// Exactly four lowercase snake_case segments: domain.module.feature.action
    /// </summary>
    public static readonly Regex FourTierCanonicalPattern = FourTierRegex();

    public static bool IsValidFourTierCanonicalCode(string? permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        if (permissionCode.Contains('*', StringComparison.Ordinal)
            || permissionCode.Contains('/', StringComparison.Ordinal)
            || permissionCode.Contains(' ', StringComparison.Ordinal)
            || permissionCode.Contains('\t', StringComparison.Ordinal))
        {
            return false;
        }

        return FourTierCanonicalPattern.IsMatch(permissionCode);
    }

    public static void EnsureValidFourTierCanonicalCode(string permissionCode)
    {
        if (!IsValidFourTierCanonicalCode(permissionCode))
        {
            throw new InvalidOperationException(
                $"Invalid Cashier POS permission code '{permissionCode}'. "
                + "Approved format is exactly four lowercase tiers: domain.module.feature.action "
                + "(no wildcards, slashes, whitespace, or empty segments).");
        }
    }

    public static void EnsureAllRoleAssignableCodesAreValid()
    {
        foreach (var definition in CashierPosCanonicalPermissionCatalog.RoleAssignable)
        {
            EnsureValidFourTierCanonicalCode(definition.Code);
        }

        var preAuth = CashierPosCanonicalPermissionCatalog.All
            .Where(d => d.Kind == CashierPosPermissionDefinitionKind.PreAuth)
            .ToList();

        if (preAuth.Count != 7)
        {
            throw new InvalidOperationException(
                $"Expected 7 pre-auth catalog entries, found {preAuth.Count}.");
        }

        if (preAuth.Any(d => d.IsRoleAssignable))
        {
            throw new InvalidOperationException(
                "Pre-auth catalog entries must not be role-assignable.");
        }
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex FourTierRegex();
}
