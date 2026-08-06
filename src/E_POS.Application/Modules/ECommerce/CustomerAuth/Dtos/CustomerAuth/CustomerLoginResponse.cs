namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed record CustomerLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    CustomerLoginCustomerDto Customer);
