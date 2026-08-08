namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public enum TenantFeatureEntitlementDecision
{
    Allowed = 0,
    Disabled = 1,
    Missing = 2,
    UnknownFeature = 3,
    Expired = 4,
    EvaluationFailed = 5
}

public sealed record TenantFeatureEntitlementEvaluation(
    TenantFeatureEntitlementDecision Decision,
    string RequestedFeatureCode,
    string CanonicalFeatureCode,
    bool UsedLegacyAlias,
    bool FoundCanonicalRecord,
    bool FoundLegacyRecord,
    string? DenialReason)
{
    public bool IsAllowed => Decision == TenantFeatureEntitlementDecision.Allowed;

    public static TenantFeatureEntitlementEvaluation Allowed(
        string requestedFeatureCode,
        string canonicalFeatureCode,
        bool usedLegacyAlias,
        bool foundCanonicalRecord,
        bool foundLegacyRecord) =>
        new(
            TenantFeatureEntitlementDecision.Allowed,
            requestedFeatureCode,
            canonicalFeatureCode,
            usedLegacyAlias,
            foundCanonicalRecord,
            foundLegacyRecord,
            null);

    public static TenantFeatureEntitlementEvaluation Denied(
        TenantFeatureEntitlementDecision decision,
        string requestedFeatureCode,
        string canonicalFeatureCode,
        bool usedLegacyAlias,
        bool foundCanonicalRecord,
        bool foundLegacyRecord,
        string denialReason) =>
        new(
            decision,
            requestedFeatureCode,
            canonicalFeatureCode,
            usedLegacyAlias,
            foundCanonicalRecord,
            foundLegacyRecord,
            denialReason);
}
