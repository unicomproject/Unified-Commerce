namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

/// <summary>
/// Fail-closed tenant feature entitlement evaluator.
/// Missing, disabled, expired, unknown, or evaluation failures all deny access.
/// Permission checks must remain separate and must never substitute for entitlement.
/// </summary>
public interface ITenantFeatureEntitlementEvaluator
{
    Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
        Guid tenantId,
        string featureCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> IsEnabledAsync(
        Guid tenantId,
        string featureCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
