using System.Net;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerTokenFactory
{
    JwtTokenResult CreateAccessToken(CustomerLoginAccount account, Guid sessionId);

    CustomerLoginPersistence CreateLoginPersistence(
        CustomerLoginAccount account,
        string? deviceName,
        bool rememberMe,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now);

    CustomerAuthTokenResult CreateTokenResult(
        CustomerLoginAccount account,
        JwtTokenResult accessToken,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        bool rememberMe);

    string CreateSecureToken();
}
