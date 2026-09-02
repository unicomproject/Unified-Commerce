namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record BusinessCapabilityMapSummaryDto(
    int BusinessModuleCount,
    int BusinessCapabilityCount,
    int TechnicalModuleCount,
    int TechnicalFeatureCount,
    int TenantPermissionCount);

public sealed record PermissionMapDto(
    string Code,
    string Name,
    string ActionType,
    string Scope,
    bool IsActive);

public sealed record TechnicalFeatureMapDto(
    Guid Id,
    string Code,
    string Name,
    string Scope,
    bool IsActive,
    string CommercialClassification,
    bool IsPlanEligible,
    IReadOnlyList<PermissionMapDto> Permissions);

public sealed record TechnicalModuleMapDto(
    string Code,
    string Name,
    string Scope,
    IReadOnlyList<TechnicalFeatureMapDto> Features);

public sealed record BusinessCapabilityMapDto(
    string Code,
    string Name,
    string Description,
    string CommercialClassification,
    IReadOnlyList<string> MappedTechnicalFeatureCodes);

public sealed record BusinessModuleMapDto(
    string Code,
    string Name,
    string Description,
    int DisplayOrder,
    string ReleaseCode,
    string CurrentR1Status,
    string CommercialState,
    IReadOnlyList<BusinessCapabilityMapDto> Capabilities,
    IReadOnlyList<TechnicalModuleMapDto> TechnicalModules);

public sealed record BusinessCapabilityMapResponseDto(
    string Release,
    string CatalogVersion,
    BusinessCapabilityMapSummaryDto Summary,
    IReadOnlyList<BusinessModuleMapDto> BusinessModules);
