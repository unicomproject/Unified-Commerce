namespace E_POS.Application.Modules.AuthSecurity.Dtos;

public sealed record TenantJwtSettings(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenMinutes,
    int RefreshTokenDays);