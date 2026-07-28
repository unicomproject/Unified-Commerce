using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// Resolves tenant create orchestration from explicit subscription type.
/// Billing cycle and subscription lifecycle status must never determine create mode.
/// </summary>
public static class TenantCreateModeResolver
{
    public enum ResolutionFailure
    {
        MissingSubscriptionType,
        UnknownSubscriptionType
    }

    public sealed record ResolutionResult(
        bool IsSuccess,
        TenantCreateMode Mode,
        ResolutionFailure? Failure = null);

    /// <summary>
    /// Wizard create requires explicit <c>subscription.subscriptionType</c> (PAID / TRIAL / DEMO).
    /// </summary>
    public static ResolutionResult ResolveWizard(string? rawSubscriptionType)
    {
        if (string.IsNullOrWhiteSpace(rawSubscriptionType))
        {
            return new ResolutionResult(
                IsSuccess: false,
                Mode: default,
                Failure: ResolutionFailure.MissingSubscriptionType);
        }

        if (!TenantSubscriptionTypeConstants.IsValid(rawSubscriptionType))
        {
            return new ResolutionResult(
                IsSuccess: false,
                Mode: default,
                Failure: ResolutionFailure.UnknownSubscriptionType);
        }

        var normalized = TenantSubscriptionTypeConstants.Normalize(rawSubscriptionType);
        return new ResolutionResult(
            IsSuccess: true,
            Mode: MapSubscriptionTypeToCreateMode(normalized));
    }

    /// <summary>
    /// Deprecated legacy minimal create (code + name + plan only): always provisions TRIAL subscription
    /// and auto-activates. Isolated from wizard classification.
    /// </summary>
    public static TenantCreateMode ResolveLegacyMinimalCompatibility() => TenantCreateMode.Trial;

    public static string InitialLifecycleStatus(TenantCreateMode mode) =>
        mode is TenantCreateMode.Trial or TenantCreateMode.Demo
            ? TenantStatusConstants.Draft
            : TenantStatusConstants.PendingPayment;

    private static TenantCreateMode MapSubscriptionTypeToCreateMode(string normalizedType) =>
        normalizedType switch
        {
            TenantSubscriptionTypeConstants.Paid => TenantCreateMode.Paid,
            TenantSubscriptionTypeConstants.Trial => TenantCreateMode.Trial,
            TenantSubscriptionTypeConstants.Demo => TenantCreateMode.Demo,
            _ => throw new ArgumentOutOfRangeException(nameof(normalizedType), normalizedType, null)
        };
}
