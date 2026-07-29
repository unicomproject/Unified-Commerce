namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed class CustomerLoginRequest
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public bool RememberMe { get; set; }
}

public sealed class CustomerRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool AgreeTerms { get; set; }
    public bool SendOffers { get; set; }
}

public sealed class CustomerVerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class CustomerResendEmailVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class CustomerForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class CustomerResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed record CustomerLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    CustomerLoginCustomerDto Customer);

public sealed record CustomerAuthTokenResult(
    CustomerLoginResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool RememberMe);

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

public sealed record CustomerPasswordResetSettings(
    string PublicStorefrontBaseUrl,
    string ResetPath);