namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformPermissionFlatResponse(
    IReadOnlyList<PlatformPermissionDto> Permissions,
    int TotalCount);

