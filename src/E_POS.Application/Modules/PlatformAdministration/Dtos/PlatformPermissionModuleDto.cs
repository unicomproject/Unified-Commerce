namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformPermissionModuleDto(
    string Key,
    string Name,
    string? Description,
    IReadOnlyList<PlatformPermissionFeatureDto> Features);
