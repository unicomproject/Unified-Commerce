namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed record CustomerJwtSettings(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenMinutes,
    int RefreshTokenDays);
