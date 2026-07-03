namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformModulesCatalogResponse(
    IReadOnlyList<PlatformModulesCatalogModuleDto> Modules);

public sealed record PlatformModulesCatalogModuleDto(
    Guid Id,
    string ModuleCode,
    string Name,
    string? Description,
    int SortOrder,
    string Status,
    IReadOnlyList<PlatformModulesCatalogFeatureDto> Features);

public sealed record PlatformModulesCatalogFeatureDto(
    Guid Id,
    string FeatureCode,
    string Name,
    string? Description,
    int SortOrder,
    string Status);
