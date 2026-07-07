namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformAdminUserDto(
    Guid Id,
    string Email,
    string Status);

