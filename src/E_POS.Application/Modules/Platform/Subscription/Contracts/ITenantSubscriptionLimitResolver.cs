using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Platform.Subscription.Contracts;

public interface ITenantSubscriptionLimitResolver
{
    Task<TenantSubscriptionLimitResolution> ResolveAsync(
        Guid tenantId,
        string limitKey,
        CancellationToken cancellationToken);
}

public sealed record TenantSubscriptionLimitResolution(
    string LimitKey,
    string Resource,
    int? PlanLimit,
    int? OverrideLimit,
    int? EffectiveLimit,
    bool IsUnlimited,
    bool OverrideApplied,
    bool IsConfigurationValid,
    string? FailureCode,
    string? FailureMessage)
{
    public static TenantSubscriptionLimitResolution ConfigurationFailure(
        string limitKey,
        string resource,
        string code,
        string message) =>
        new(limitKey, resource, null, null, null, false, false, false, code, message);
}

public interface ITenantResourceLimitGuard
{
    Task<TenantResourceLimitEvaluation> EvaluateAsync(
        Guid tenantId,
        string limitKey,
        int requestedIncrease,
        CancellationToken cancellationToken);

    /// <summary>
    /// Acquires a tenant+limit advisory lock (PostgreSQL), evaluates capacity, runs <paramref name="operation"/>,
    /// then syncs the usage counter when the operation requests commit.
    /// For InMemory/non-relational providers, evaluates without advisory lock.
    /// </summary>
    Task<TenantResourceLimitGuardResult<T>> ExecuteWithinCapacityAsync<T>(
        Guid tenantId,
        string limitKey,
        int requestedIncrease,
        Func<CancellationToken, Task<TenantResourceCapacityOperationResult<T>>> operation,
        CancellationToken cancellationToken);

    Task<TenantResourceCapacitySnapshot> GetCapacitySnapshotAsync(
        Guid tenantId,
        string limitKey,
        CancellationToken cancellationToken);
}

public sealed record TenantResourceCapacityOperationResult<T>(bool Commit, T Value)
{
    public static TenantResourceCapacityOperationResult<T> Succeeded(T value) => new(true, value);

    public static TenantResourceCapacityOperationResult<T> Aborted(T value) => new(false, value);
}

public sealed record TenantResourceLimitEvaluation(
    string LimitKey,
    string Resource,
    int CurrentUsage,
    int RequestedIncrease,
    int? EffectiveLimit,
    int? RemainingCapacity,
    bool IsUnlimited,
    bool Allowed,
    bool OverrideApplied,
    string? FailureCode,
    string? FailureMessage)
{
    public ApplicationError? ToApplicationError()
    {
        if (Allowed || string.IsNullOrWhiteSpace(FailureCode))
        {
            return null;
        }

        var fields = new List<ApplicationFieldError>
        {
            new("resource", Resource),
            new("limitKey", LimitKey),
            new("currentUsage", CurrentUsage.ToString()),
            new("requestedIncrease", RequestedIncrease.ToString()),
            new("remainingCapacity", (RemainingCapacity ?? 0).ToString())
        };

        if (EffectiveLimit.HasValue)
        {
            fields.Add(new ApplicationFieldError("effectiveLimit", EffectiveLimit.Value.ToString()));
        }

        if (IsUnlimited)
        {
            fields.Add(new ApplicationFieldError("unlimited", "true"));
        }

        fields.Add(new ApplicationFieldError("overrideApplied", OverrideApplied ? "true" : "false"));

        return new ApplicationError(
            FailureCode,
            FailureMessage ?? "Subscription limit evaluation failed.",
            fields);
    }
}

public sealed record TenantResourceCapacitySnapshot(
    string LimitKey,
    string Resource,
    int CurrentUsage,
    int? EffectiveLimit,
    int? RemainingCapacity,
    bool IsUnlimited,
    bool CanCreate,
    bool OverrideApplied);

public sealed record TenantResourceLimitGuardResult<T>(
    bool Allowed,
    T? Value,
    TenantResourceLimitEvaluation Evaluation)
{
    public static TenantResourceLimitGuardResult<T> Denied(TenantResourceLimitEvaluation evaluation) =>
        new(false, default, evaluation);

    public static TenantResourceLimitGuardResult<T> Succeeded(T value, TenantResourceLimitEvaluation evaluation) =>
        new(true, value, evaluation);
}

public static class SubscriptionLimitErrorCodes
{
    public const string LimitReached = "subscription_limit_reached";
    public const string ConfigurationMissing = "subscription_limit_configuration_missing";
    public const string Invalid = "subscription_limit_invalid";
    public const string EvaluationFailed = "subscription_limit_evaluation_failed";
    public const string UnknownKey = "subscription_limit_unknown_key";
    public const string NotEnforced = "subscription_limit_not_enforced";
}
