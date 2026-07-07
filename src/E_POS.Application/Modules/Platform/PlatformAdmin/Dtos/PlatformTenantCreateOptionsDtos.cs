namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformTenantCreateOptionsResponse(
    IReadOnlyList<PlatformTenantCreatePlanOptionDto> Plans,
    IReadOnlyList<PlatformTenantCreateAddonOptionDto> Addons,
    IReadOnlyList<PlatformTenantCreateCatalogModuleDto> CatalogModules,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> BillingStatuses,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> PaymentMethods,
    IReadOnlyList<PlatformTenantCreateCountryOptionDto> CountryCodes,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> Currencies,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> Timezones,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> Locales,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> BusinessTypes,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> OperatingModes,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> SubscriptionStatuses,
    IReadOnlyList<PlatformTenantCreateLookupOptionDto> BillingCycles);

public sealed record PlatformTenantCreatePlanOptionDto(
    Guid Id,
    string PlanCode,
    string Name,
    string? Description,
    string Status,
    string BillingCycle,
    string BaseCurrency,
    decimal BasePrice,
    int? MaxOutlets,
    int? MaxTills,
    int? MaxUsers,
    IReadOnlyList<Guid> IncludedFeatureIds,
    IReadOnlyList<string> IncludedFeatureCodes);

public sealed record PlatformTenantCreateAddonOptionDto(
    Guid Id,
    string AddonCode,
    string Name,
    string? Description,
    decimal UnitPrice,
    string Currency,
    string? RelatedFeatureCode,
    IReadOnlyDictionary<string, int> LimitIncrementByKey);

public sealed record PlatformTenantCreateCatalogModuleDto(
    Guid Id,
    string ModuleCode,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<PlatformTenantCreateCatalogFeatureDto> Features);

public sealed record PlatformTenantCreateCatalogFeatureDto(
    Guid Id,
    string FeatureCode,
    string Name,
    string? Description,
    int SortOrder);

public sealed record PlatformTenantCreateCountryOptionDto(
    string Code,
    string Name);

public sealed record PlatformTenantCreateLookupOptionDto(
    string Value,
    string Label);

