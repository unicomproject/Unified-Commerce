namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformJwtSettings(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenMinutes,
    int RefreshTokenDays);