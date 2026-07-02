namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformPermissionFeatureDto(
    string Key,
    string Name,
    string? Description,
    IReadOnlyList<PlatformPermissionDto> Permissions);
