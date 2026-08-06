using System.Net;
using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Mappers;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

public sealed class CustomerTokenFactory : ICustomerTokenFactory
{
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHashService _tokenHashService;
    private readonly CustomerJwtSettings _jwtSettings;

    public CustomerTokenFactory(
        IJwtTokenFactory jwtTokenFactory,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHashService tokenHashService,
        CustomerJwtSettings jwtSettings)
    {
        _jwtTokenFactory = jwtTokenFactory;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHashService = tokenHashService;
        _jwtSettings = jwtSettings;
    }

    public JwtTokenResult CreateAccessToken(CustomerLoginAccount account, Guid sessionId)
    {
        return _jwtTokenFactory.CreateAccessToken(new JwtTokenDescriptor(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            _jwtSettings.SigningKey,
            _jwtSettings.AccessTokenMinutes,
            new Dictionary<string, object>
            {
                ["sub"] = account.CustomerId.ToString(),
                ["tenant_id"] = account.TenantId.ToString(),
                ["session_id"] = sessionId.ToString(),
                ["auth_account_id"] = account.Account.Id.ToString(),
                ["identity_type"] = "customer",
                ["jti"] = Guid.NewGuid().ToString("N"),
                ["email"] = account.Email ?? string.Empty
            }));
    }

    public CustomerLoginPersistence CreateLoginPersistence(
        CustomerLoginAccount account,
        string? deviceName,
        bool rememberMe,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        var sessionId = Guid.NewGuid();
        var accessToken = CreateAccessToken(account, sessionId);
        var refreshToken = _refreshTokenGenerator.CreateRefreshToken(_jwtSettings.RefreshTokenDays);
        var session = CustomerAuthSession.Create(
            sessionId,
            account.TenantId,
            account.Account.Id,
            _tokenHashService.HashToken(sessionId.ToString("N"), _jwtSettings.SigningKey),
            ipAddress,
            userAgent,
            deviceName,
            refreshToken.ExpiresAt,
            now);
        var refreshTokenEntity = CustomerRefreshToken.Create(
            Guid.NewGuid(),
            account.TenantId,
            sessionId,
            _tokenHashService.HashToken(refreshToken.Token, _jwtSettings.SigningKey),
            Guid.NewGuid(),
            refreshToken.ExpiresAt,
            now);

        return new CustomerLoginPersistence(
            CreateTokenResult(account, accessToken, refreshToken.Token, refreshToken.ExpiresAt, rememberMe),
            session,
            refreshTokenEntity);
    }

    public CustomerAuthTokenResult CreateTokenResult(
        CustomerLoginAccount account,
        JwtTokenResult accessToken,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        bool rememberMe) =>
        CustomerAuthTokenMapper.ToTokenResult(account, accessToken, refreshToken, refreshTokenExpiresAt, rememberMe);

    public string CreateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
