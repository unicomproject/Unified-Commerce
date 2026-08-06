using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Mappers;

public static class CustomerAuthTokenMapper
{
    public static CustomerLoginCustomerDto ToCustomerDto(CustomerLoginAccount account) => new(
        account.CustomerId,
        account.TenantId,
        account.DisplayName,
        account.Email,
        account.Phone);

    public static CustomerLoginResponse ToLoginResponse(
        CustomerLoginAccount account,
        JwtTokenResult accessToken) => new(
        accessToken.AccessToken,
        accessToken.ExpiresAt,
        ToCustomerDto(account));

    public static CustomerAuthTokenResult ToTokenResult(
        CustomerLoginAccount account,
        JwtTokenResult accessToken,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        bool rememberMe) => new(
        ToLoginResponse(account, accessToken),
        refreshToken,
        refreshTokenExpiresAt,
        rememberMe);
}
