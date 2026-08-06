using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Infrastructure.Modules.Platform.Subscription.Entitlements;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Platform.Subscription.Services;

/// <summary>
/// Phase 1 Strategy B evaluator:
/// - New/canonical authority key for outlets is <c>outlet_management</c>.
/// - Legacy <c>tenant_admin.outlets</c> is recognized only when no canonical entitlement row exists.
/// - Canonical record always wins when both exist.
/// - Missing/unknown/disabled/expired/errors fail closed.
/// </summary>
public sealed class TenantFeatureEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
{
    private readonly EPosDbContext _dbContext;
    private readonly ILogger<TenantFeatureEntitlementEvaluator> _logger;

    public TenantFeatureEntitlementEvaluator(
        EPosDbContext dbContext,
        ILogger<TenantFeatureEntitlementEvaluator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(
        Guid tenantId,
        string featureCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var evaluation = await EvaluateAsync(tenantId, featureCode, now, cancellationToken);
        return evaluation.IsAllowed;
    }

    public async Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
        Guid tenantId,
        string featureCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(featureCode))
            {
                return DenyUnknown(string.Empty, string.Empty, usedLegacyAlias: false, foundCanonical: false, foundLegacy: false);
            }

            var requested = featureCode.Trim();
            var hasKnownMapping = PlatformTenantFeatureCodes.TryGetCanonicalFeatureCode(requested, out var canonical);
            if (!hasKnownMapping)
            {
                // Unknown to the registry: still check DB for an active catalog row under the raw key.
                // If neither registry nor catalog knows it, deny as unknown.
                canonical = requested;
            }

            var lookupCodes = PlatformTenantFeatureCodes.GetLookupFeatureCodes(requested);
            var featureRows = await _dbContext.PlatformFeatures
                .AsNoTracking()
                .Where(x => lookupCodes.Contains(x.FeatureCode) &&
                            x.Status == SubscriptionCatalogConstants.RecordStatus.Active)
                .Select(x => new { x.Id, x.FeatureCode })
                .ToListAsync(cancellationToken);

            var canonicalFeature = featureRows.FirstOrDefault(x =>
                string.Equals(x.FeatureCode, canonical, StringComparison.OrdinalIgnoreCase));
            var legacyFeature = featureRows.FirstOrDefault(x =>
                !string.Equals(x.FeatureCode, canonical, StringComparison.OrdinalIgnoreCase));

            var foundCanonical = canonicalFeature is not null;
            var foundLegacy = legacyFeature is not null;

            if (!foundCanonical && !foundLegacy)
            {
                LogDenial(
                    tenantId,
                    requested,
                    canonical,
                    TenantFeatureEntitlementDecision.UnknownFeature,
                    usedLegacyAlias: false,
                    foundCanonical,
                    foundLegacy);
                return DenyUnknown(requested, canonical, usedLegacyAlias: false, foundCanonical, foundLegacy);
            }

            if (foundCanonical && foundLegacy)
            {
                _logger.LogWarning(
                    "Tenant {TenantId} has both canonical feature {CanonicalFeatureCode} and legacy alias entitlement candidates for requested key {RequestedFeatureCode}. Canonical record wins.",
                    tenantId,
                    canonical,
                    requested);
            }

            if (foundCanonical)
            {
                var canonicalDecision = await EvaluateFeatureEntitlementAsync(
                    tenantId,
                    canonicalFeature!.Id,
                    now,
                    cancellationToken);

                if (canonicalDecision is TenantFeatureEntitlementDecision.Allowed)
                {
                    return TenantFeatureEntitlementEvaluation.Allowed(
                        requested,
                        canonical,
                        usedLegacyAlias: false,
                        foundCanonicalRecord: true,
                        foundLegacyRecord: foundLegacy);
                }

                // Canonical entitlement row exists but is not allowed — never fall through to legacy.
                if (canonicalDecision is not TenantFeatureEntitlementDecision.Missing)
                {
                    LogDenial(tenantId, requested, canonical, canonicalDecision, usedLegacyAlias: false, foundCanonical, foundLegacy);
                    return TenantFeatureEntitlementEvaluation.Denied(
                        canonicalDecision,
                        requested,
                        canonical,
                        usedLegacyAlias: false,
                        foundCanonicalRecord: true,
                        foundLegacyRecord: foundLegacy,
                        denialReason: ToDenialReason(canonicalDecision));
                }

                // Canonical feature exists in catalog but tenant has no entitlement row.
                // Only then may we consult an approved legacy alias entitlement.
            }

            if (foundLegacy)
            {
                var legacyDecision = await EvaluateFeatureEntitlementAsync(
                    tenantId,
                    legacyFeature!.Id,
                    now,
                    cancellationToken);

                if (legacyDecision is TenantFeatureEntitlementDecision.Allowed)
                {
                    _logger.LogInformation(
                        "Tenant {TenantId} outlet entitlement resolved via legacy alias for requested key {RequestedFeatureCode}; canonical key is {CanonicalFeatureCode}.",
                        tenantId,
                        requested,
                        canonical);
                    return TenantFeatureEntitlementEvaluation.Allowed(
                        requested,
                        canonical,
                        usedLegacyAlias: true,
                        foundCanonicalRecord: foundCanonical,
                        foundLegacyRecord: true);
                }

                LogDenial(tenantId, requested, canonical, legacyDecision, usedLegacyAlias: true, foundCanonical, foundLegacy);
                return TenantFeatureEntitlementEvaluation.Denied(
                    legacyDecision == TenantFeatureEntitlementDecision.Missing
                        ? TenantFeatureEntitlementDecision.Missing
                        : legacyDecision,
                    requested,
                    canonical,
                    usedLegacyAlias: true,
                    foundCanonicalRecord: foundCanonical,
                    foundLegacyRecord: true,
                    denialReason: ToDenialReason(legacyDecision));
            }

            LogDenial(tenantId, requested, canonical, TenantFeatureEntitlementDecision.Missing, usedLegacyAlias: false, foundCanonical, foundLegacy);
            return TenantFeatureEntitlementEvaluation.Denied(
                TenantFeatureEntitlementDecision.Missing,
                requested,
                canonical,
                usedLegacyAlias: false,
                foundCanonicalRecord: foundCanonical,
                foundLegacyRecord: foundLegacy,
                denialReason: "Tenant entitlement record is missing.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Entitlement evaluation failed for tenant {TenantId} feature {RequestedFeatureCode}. Failing closed.",
                tenantId,
                featureCode);
            return TenantFeatureEntitlementEvaluation.Denied(
                TenantFeatureEntitlementDecision.EvaluationFailed,
                featureCode?.Trim() ?? string.Empty,
                PlatformTenantFeatureCodes.NormalizeToCanonicalOrSelf(featureCode ?? string.Empty),
                usedLegacyAlias: false,
                foundCanonicalRecord: false,
                foundLegacyRecord: false,
                denialReason: "Entitlement evaluation failed.");
        }
    }

    private async Task<TenantFeatureEntitlementDecision> EvaluateFeatureEntitlementAsync(
        Guid tenantId,
        Guid platformFeatureId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.TenantFeatureEntitlements
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PlatformFeatureId == platformFeatureId)
            .Select(x => new
            {
                x.EntitlementStatus,
                x.IsEnabled,
                x.RevokedAt,
                x.EffectiveFrom,
                x.EffectiveUntil
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return TenantFeatureEntitlementDecision.Missing;
        }

        if (rows.Any(x => TenantEntitlementEffectivePredicate.IsEnabled(
                x.EntitlementStatus,
                x.IsEnabled,
                x.RevokedAt,
                x.EffectiveFrom,
                x.EffectiveUntil,
                now)))
        {
            return TenantFeatureEntitlementDecision.Allowed;
        }

        if (rows.Any(x => string.Equals(x.EntitlementStatus, TenantEntitlementStatusConstants.Expired, StringComparison.OrdinalIgnoreCase) ||
                          (x.EffectiveUntil is not null && x.EffectiveUntil <= now)))
        {
            return TenantFeatureEntitlementDecision.Expired;
        }

        return TenantFeatureEntitlementDecision.Disabled;
    }

    private void LogDenial(
        Guid tenantId,
        string requestedFeatureCode,
        string canonicalFeatureCode,
        TenantFeatureEntitlementDecision decision,
        bool usedLegacyAlias,
        bool foundCanonicalRecord,
        bool foundLegacyRecord)
    {
        _logger.LogWarning(
            "Entitlement denied for tenant {TenantId}. Requested={RequestedFeatureCode}, Canonical={CanonicalFeatureCode}, Decision={Decision}, UsedLegacyAlias={UsedLegacyAlias}, FoundCanonical={FoundCanonical}, FoundLegacy={FoundLegacy}",
            tenantId,
            requestedFeatureCode,
            canonicalFeatureCode,
            decision,
            usedLegacyAlias,
            foundCanonicalRecord,
            foundLegacyRecord);
    }

    private static TenantFeatureEntitlementEvaluation DenyUnknown(
        string requested,
        string canonical,
        bool usedLegacyAlias,
        bool foundCanonical,
        bool foundLegacy) =>
        TenantFeatureEntitlementEvaluation.Denied(
            TenantFeatureEntitlementDecision.UnknownFeature,
            requested,
            canonical,
            usedLegacyAlias,
            foundCanonical,
            foundLegacy,
            "Unknown feature key.");

    private static string ToDenialReason(TenantFeatureEntitlementDecision decision) =>
        decision switch
        {
            TenantFeatureEntitlementDecision.Disabled => "Tenant entitlement is disabled.",
            TenantFeatureEntitlementDecision.Expired => "Tenant entitlement is expired.",
            TenantFeatureEntitlementDecision.Missing => "Tenant entitlement record is missing.",
            TenantFeatureEntitlementDecision.UnknownFeature => "Unknown feature key.",
            TenantFeatureEntitlementDecision.EvaluationFailed => "Entitlement evaluation failed.",
            _ => "Tenant is not entitled to this feature."
        };
}
