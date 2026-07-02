namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformPermissionCatalogResponse(
    IReadOnlyList<PlatformPermissionModuleDto> Modules);
