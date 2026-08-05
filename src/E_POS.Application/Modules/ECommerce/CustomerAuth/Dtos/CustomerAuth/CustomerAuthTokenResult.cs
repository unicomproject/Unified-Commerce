namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed record CustomerAuthTokenResult(
    CustomerLoginResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool RememberMe);
