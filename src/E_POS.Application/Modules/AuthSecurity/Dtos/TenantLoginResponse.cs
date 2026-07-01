namespace E_POS.Application.Modules.AuthSecurity.Dtos;

public sealed record TenantLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    TenantLoginUserDto User,
    IReadOnlyList<string> Permissions);