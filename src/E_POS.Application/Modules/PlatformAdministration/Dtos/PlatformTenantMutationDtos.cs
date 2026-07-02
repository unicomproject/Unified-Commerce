namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record CreatePlatformTenantRequest
{
    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? LegalName { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? TaxNumber { get; init; }

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
}

public sealed record UpdatePlatformTenantRequest
{
    public string? Name { get; init; }

    public string? BaseCurrency { get; init; }

    public string? DefaultTimezone { get; init; }

    public string? DefaultLocale { get; init; }

    public string? OperatingMode { get; init; }

    public string? BusinessType { get; init; }

    public string? BillingStatus { get; init; }
}

public sealed record UpdatePlatformTenantEntitlementsRequest
{
    public Guid? SubscriptionPlanId { get; init; }

    public IReadOnlyList<Guid>? EnabledFeatureIds { get; init; }

    public IReadOnlyList<string>? EnabledFeatureCodes { get; init; }
}

public sealed record ResolvedTenantFeature(
    Guid Id,
    string FeatureCode);
