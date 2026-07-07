namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformJwtSettings(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenMinutes,
    int RefreshTokenDays);
