namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed class CustomerLoginRequest
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
}

public sealed record CustomerLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    CustomerLoginCustomerDto Customer);

public sealed record CustomerAuthTokenResult(
    CustomerLoginResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record CustomerLoginCustomerDto(
    Guid Id,
    Guid TenantId,
    string DisplayName,
    string? Email,
    string? Phone);

public sealed record CustomerJwtSettings(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenMinutes,
    int RefreshTokenDays);
