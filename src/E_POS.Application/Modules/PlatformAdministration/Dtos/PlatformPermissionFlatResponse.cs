namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformPermissionFlatResponse(
    IReadOnlyList<PlatformPermissionDto> Permissions,
    int TotalCount);
