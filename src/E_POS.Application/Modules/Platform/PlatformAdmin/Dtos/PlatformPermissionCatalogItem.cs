namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformPermissionCatalogItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status);

