namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

using System.Text.Json.Serialization;

public sealed record CreatePlatformTenantRequest
{
    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? TenantSlug { get; init; }

    public string? RequestedSubdomain { get; init; }

    public string? LegalName { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? TaxNumber { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? BaseCurrency { get; init; }

    public string? DefaultTimezone { get; init; }

    public string? DefaultLocale { get; init; }

    public string? OperatingMode { get; init; }

    public string? BusinessType { get; init; }

    public string? CountryCode { get; init; }

    public string? BillingStatus { get; init; }

    public Guid? SubscriptionPlanId { get; init; }

    public CreatePlatformTenantAddressRequest? Address { get; init; }

    public CreatePlatformTenantContactRequest? PrimaryContact { get; init; }

    public CreatePlatformTenantLimitsRequest? Limits { get; init; }

    public IReadOnlyList<CreatePlatformTenantAddonSelectionRequest>? Addons { get; init; }

    public IReadOnlyList<Guid>? EnabledFeatureIds { get; init; }

    public IReadOnlyList<string>? EnabledFeatureCodes { get; init; }

    public CreatePlatformTenantAdminRequest? TenantAdmin { get; init; }

    public CreatePlatformTenantSubscriptionDetailsRequest? Subscription { get; init; }

    [JsonIgnore]
    public PlatformTenantOnboardingFinalizeContext? OnboardingFinalizeContext { get; init; }
}

public sealed record PlatformTenantOnboardingFinalizeContext(
    Guid DraftId,
    long ExpectedDraftVersion,
    Guid OperationId,
    string IdempotencyKeyHash,
    string RequestHash,
    bool RequiresPayment,
    IReadOnlyList<PlatformTenantOnboardingContactWriteDto> Contacts,
    Guid ActorPlatformUserId,
    DateTimeOffset RequestedAt);

public sealed record PlatformTenantOnboardingContactWriteDto(
    string ContactType,
    string ContactName,
    string? Email,
    string? Phone);

public sealed record UpdatePlatformTenantRequest
{
    public string? Name { get; init; }

    public string? BaseCurrency { get; init; }

    public string? DefaultTimezone { get; init; }

    public string? DefaultLocale { get; init; }

    public string? OperatingMode { get; init; }

    public string? BusinessType { get; init; }

    public string? BillingStatus { get; init; }

    public string? ConcurrencyVersion { get; init; }
}

public sealed record UpdatePlatformTenantEntitlementsRequest
{
    public Guid? SubscriptionPlanId { get; init; }

    public IReadOnlyList<Guid>? EnabledFeatureIds { get; init; }

    public IReadOnlyList<string>? EnabledFeatureCodes { get; init; }

    public string? SourceType { get; init; }

    public string? OverrideReason { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveUntil { get; init; }

    public string? ConcurrencyVersion { get; init; }
}

public sealed record ResolvedTenantFeature(
    Guid Id,
    string FeatureCode);

