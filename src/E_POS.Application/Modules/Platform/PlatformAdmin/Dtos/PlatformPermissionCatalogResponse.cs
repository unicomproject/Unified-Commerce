namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformPermissionCatalogResponse(
    IReadOnlyList<PlatformPermissionModuleDto> Modules);

