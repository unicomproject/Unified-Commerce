namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformModulesCatalogResponse(
    IReadOnlyList<PlatformModulesCatalogModuleDto> Modules);

public sealed record PlatformModulesCatalogModuleDto(
    Guid Id,
    string ModuleCode,
    string Name,
    string? Description,
    int SortOrder,
    string Status,
    IReadOnlyList<PlatformModulesCatalogFeatureDto> Features,
    string Scope = "TENANT");

public sealed record PlatformModulesCatalogFeatureDto(
    Guid Id,
    string FeatureCode,
    string Name,
    string? Description,
    int SortOrder,
    string Status,
    IReadOnlyList<PlatformModulesCatalogPermissionDto> Permissions,
    string Scope = "TENANT");

public sealed record PlatformModulesCatalogPermissionDto(
    Guid Id,
    string PermissionCode,
    string Name,
    string? Description,
    string ActionType,
    string Scope = "TENANT",
    bool IsActive = true);


