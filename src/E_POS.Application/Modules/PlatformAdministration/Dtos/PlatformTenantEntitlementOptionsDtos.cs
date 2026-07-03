namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformTenantEntitlementOptionsResponse(
    Guid TenantId,
    Guid? CurrentSubscriptionPlanId,
    string? CurrentSubscriptionPlanCode,
    string? CurrentSubscriptionPlanName,
    IReadOnlyList<Guid> EnabledFeatureIds,
    IReadOnlyList<string> EnabledFeatureCodes,
    IReadOnlyList<PlatformTenantEntitlementPlanOptionDto> Plans,
    IReadOnlyList<PlatformTenantEntitlementCatalogModuleDto> CatalogModules);

public sealed record PlatformTenantEntitlementPlanOptionDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    IReadOnlyList<Guid> IncludedFeatureIds,
    IReadOnlyList<string> IncludedFeatureCodes);

public sealed record PlatformTenantEntitlementCatalogModuleDto(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyList<PlatformTenantEntitlementCatalogFeatureDto> Features);

public sealed record PlatformTenantEntitlementCatalogFeatureDto(
    Guid Id,
    string Code,
    string Name,
    string? Description);
