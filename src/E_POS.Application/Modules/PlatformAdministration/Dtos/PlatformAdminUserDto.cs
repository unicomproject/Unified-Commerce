namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformAdminUserDto(
    Guid Id,
    string Email,
    string Status);
