using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Queries;

/// <summary>
/// Chunk 5: bulk-load candidate grants, apply tenant entitlement ceiling,
/// then resolve parent/child effective permissions in memory.
/// </summary>
internal static class TenantEffectivePermissionResolution
{
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        EPosDbContext dbContext,
        Guid tenantUserId,
        Guid tenantId,
        DateTimeOffset now,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var candidates = await TenantEffectivePermissionCodesQuery
            .Build(dbContext, tenantUserId, tenantId)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<string>();
        }

        var enabledFeatureIds = await LoadEnabledFeatureIdsAsync(
            dbContext,
            tenantId,
            now,
            cancellationToken);

        var normalizedCandidates = candidates
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var definitionRows = await (
            from permission in dbContext.PermissionDefinitions.AsNoTracking()
            join feature in dbContext.PlatformFeatures.AsNoTracking()
                on permission.FeatureId equals feature.Id
            where normalizedCandidates.Contains(permission.PermissionCode)
            select new PermissionFeatureRow(
                permission.PermissionCode,
                permission.IsActive,
                feature.Id,
                feature.FeatureCode,
                feature.IsCoreFeature))
            .ToListAsync(cancellationToken);

        var definitionByCode = definitionRows
            .ToDictionary(row => row.PermissionCode, StringComparer.OrdinalIgnoreCase);

        var entitledCandidates = new List<string>(definitionRows.Count);
        foreach (var row in definitionRows)
        {
            if (!row.IsActive)
            {
                logger?.LogDebug(
                    "Effective permission skipped inactive definition. TenantId={TenantId} UserId={UserId} Code={PermissionCode}",
                    tenantId,
                    tenantUserId,
                    row.PermissionCode);
                continue;
            }

            if (!HasRequiredEntitlement(
                    row.PermissionCode,
                    row.FeatureId,
                    row.FeatureCode,
                    row.IsCoreFeature,
                    enabledFeatureIds))
            {
                logger?.LogDebug(
                    "Effective permission skipped by tenant entitlement ceiling. TenantId={TenantId} UserId={UserId} Code={PermissionCode} Feature={FeatureCode}",
                    tenantId,
                    tenantUserId,
                    row.PermissionCode,
                    row.FeatureCode);
                continue;
            }

            entitledCandidates.Add(row.PermissionCode);
        }

        foreach (var code in normalizedCandidates)
        {
            if (definitionByCode.ContainsKey(code))
            {
                continue;
            }

            // Granted code missing from permission_definitions join → fail closed.
            logger?.LogWarning(
                "Effective permission skipped unknown stored code. TenantId={TenantId} UserId={UserId} Code={PermissionCode}",
                tenantId,
                tenantUserId,
                code);
        }

        return CashierPosEffectivePermissionResolver.Resolve(entitledCandidates);
    }

    private static async Task<HashSet<Guid>> LoadEnabledFeatureIdsAsync(
        EPosDbContext dbContext,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.TenantFeatureEntitlements
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .Select(item => new
            {
                item.PlatformFeatureId,
                item.EntitlementStatus,
                item.IsEnabled,
                item.RevokedAt,
                item.EffectiveFrom,
                item.EffectiveUntil
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(item => TenantEntitlementEffectivePredicate.IsEnabled(
                item.EntitlementStatus,
                item.IsEnabled,
                item.RevokedAt,
                item.EffectiveFrom,
                item.EffectiveUntil,
                now))
            .Select(item => item.PlatformFeatureId)
            .ToHashSet();
    }

    private static bool HasRequiredEntitlement(
        string permissionCode,
        Guid featureId,
        string featureCode,
        bool isCoreFeature,
        HashSet<Guid> enabledFeatureIds)
    {
        if (isCoreFeature ||
            TenantAdminBootstrapPermissionCatalog.BasePermissionCodes.Contains(
                permissionCode,
                StringComparer.OrdinalIgnoreCase) ||
            !PlatformTenantFeatureCodes.IsKnownFeatureCode(featureCode))
        {
            return true;
        }

        return enabledFeatureIds.Contains(featureId);
    }

    private sealed record PermissionFeatureRow(
        string PermissionCode,
        bool IsActive,
        Guid FeatureId,
        string FeatureCode,
        bool IsCoreFeature);
}
