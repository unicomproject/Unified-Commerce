namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformPermissionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status,
    bool IsSystem,
    bool IsBootstrap);
